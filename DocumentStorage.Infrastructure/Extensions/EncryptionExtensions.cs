using DocumentStorage.Domain;
using DocumentStorage.Domain.Attributes;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DocumentStorage.Infrastructure;

public static class EncryptionExtensions
{
    /// <summary>
    /// Decrypts the properties of the specified entity that are marked with the <see cref="DsEncryptedColumnAttribute"/>.
    /// This extension method is for models implementing the <see cref="IDsRecord"/> interface.
    /// </summary>
    /// <param name="entity">The entity object whose encrypted properties will be decrypted.</param>
    /// <param name="encryptionService">The encryption provider used to perform the decryption of the entity's properties.</param>
    /// <returns>The updated entity object with decrypted property values.</returns>
    public static object DecryptEntity(this object entity, IEncryptionProvider encryptionService)
    {
        if (entity is not IDsRecord) return entity;

        var encryptedProperties = entity.GetType().GetProperties()
            .Where(p => p.GetCustomAttributes(typeof(DsEncryptedColumnAttribute), true).Any(_ => p.PropertyType == typeof(string)));

        foreach (var property in encryptedProperties)
        {
            DsEncryptedColumnAttribute attr = (DsEncryptedColumnAttribute)property.GetCustomAttribute(typeof(DsEncryptedColumnAttribute), true);
            string encryptedValue = property.GetValue(entity) as string;
            if (!string.IsNullOrEmpty(encryptedValue))
            {
                string value = attr.Searchable ? encryptionService.DecryptDeterministic(encryptedValue) : encryptionService.Decrypt(encryptedValue);
                entity.GetType().GetProperty(property.Name).SetValue(entity, value);
            }
        }

        return entity;
    }

    /// <summary>
    /// Encrypts the properties of the specified entity that are marked with the <see cref="DsEncryptedColumnAttribute"/>.
    /// This extension method is for models implementing the <see cref="IDsRecord"/> interface.
    /// </summary>
    /// <param name="entity">The entity object whose properties marked for encryption will be encrypted.</param>
    /// <param name="encryptionService">The encryption provider used to perform the encryption of the entity's properties.</param>
    /// <returns>The updated entity object with encrypted property values.</returns>
    public static object EncryptEntity(this object entity, IEncryptionProvider encryptionService)
    {
        if (entity is not IDsRecord) return entity;

        var encryptedProperties = entity.GetType().GetProperties()
            .Where(p => p.GetCustomAttributes(typeof(DsEncryptedColumnAttribute), true).Any(_ => p.PropertyType == typeof(string)));
        foreach (var property in encryptedProperties)
        {
            DsEncryptedColumnAttribute attr = (DsEncryptedColumnAttribute)property.GetCustomAttribute(typeof(DsEncryptedColumnAttribute), true);
            string value = property.GetValue(entity) as string;
            if (!string.IsNullOrEmpty(value))
            {
                string encryptedValue = attr.Searchable ? encryptionService.EncryptDeterministic(value) : encryptionService.Encrypt(value);
                property.SetValue(entity, encryptedValue);
            }
        }

        return entity;
    }

    /// <summary>
    /// Modifies the provided expression to handle encryption for properties marked with the <see cref="DsEncryptedColumnAttribute"/>.
    /// This extension method utilizes an <see cref="EncryptionExpressionVisitor{T}"/> to traverse and modify the expression tree,
    /// ensuring that any encrypted properties are correctly processed by the specified encryption provider.
    /// </summary>
    /// <typeparam name="T">The type of the entity implementing the <see cref="IDsRecord"/> interface.</typeparam>
    /// <param name="expression">The expression to be modified, which is expected to represent a predicate for filtering entities.</param>
    /// <param name="provider">The encryption provider used for handling encryption-related logic within the expression.</param>
    /// <returns>A modified expression that incorporates encryption logic for applicable properties.</returns>
    public static Expression<Func<T, bool>> ModifyExpressionForEncryption<T>(this Expression<Func<T, bool>> expression, IEncryptionProvider provider) where T : IDsRecord =>
        (Expression<Func<T, bool>>)new EncryptionExpressionVisitor<T>(provider).Visit(expression);
}

/// <summary>
/// A custom <see cref="ExpressionVisitor"/> that modifies expression trees to handle encryption
/// for properties marked with the <see cref="DsEncryptedColumnAttribute"/>.
/// This class intercepts binary expressions and replaces the right-hand side operand with its
/// encrypted value using the specified <see cref="IEncryptionProvider"/>.
/// </summary>
/// <typeparam name="T">The type of the entity that the expression is visiting, which must implement the <see cref="IDsRecord"/> interface.</typeparam>
public class EncryptionExpressionVisitor<T> : ExpressionVisitor
{
    private readonly IEncryptionProvider _encryptionProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptionExpressionVisitor{T}"/> class.
    /// </summary>
    /// <param name="encryptionProvider">The encryption provider used to encrypt values in the expression.</param>
    public EncryptionExpressionVisitor(IEncryptionProvider encryptionProvider) => _encryptionProvider = encryptionProvider;

    /// <summary>
    /// Visits a binary expression node and replaces the right operand with its encrypted value if the left operand
    /// is a member with the <see cref="DsEncryptedColumnAttribute"/> attribute.
    /// </summary>
    /// <param name="node">The binary expression node to visit.</param>
    /// <returns>The modified binary expression with the encrypted value, or the original node if no modifications are needed.</returns>
    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (node.Left is MemberExpression memberExpression)
        {
            var member = memberExpression.Member;
            var attr = member.GetCustomAttribute<DsEncryptedColumnAttribute>();

            if (attr is not null)
            {
                var rightValue = EvaluateExpression(node.Right);
                var encryptedValue = attr.Searchable ? _encryptionProvider.EncryptDeterministic(rightValue.ToString()) : _encryptionProvider.Encrypt(rightValue.ToString());
                var encryptedConstant = Expression.Constant(encryptedValue);

                return Expression.MakeBinary(node.NodeType, node.Left, encryptedConstant);
            }
        }

        return base.VisitBinary(node);
    }

    private object EvaluateExpression(Expression expression)
    {
        if (expression is ConstantExpression constantExpression)
        {
            return constantExpression.Value;
        }
        else if (expression is MemberExpression memberExpression)
        {
            var objectMember = Expression.Convert(memberExpression, typeof(object));
            var getterLambda = Expression.Lambda<Func<object>>(objectMember);
            var getter = getterLambda.Compile();
            return getter();
        }
        else if (expression is ParameterExpression parameterExpression)
        {
            throw new NotSupportedException("ParameterExpressions cannot be evaluated directly.");
        }
        else if (expression is MethodCallExpression methodCallExpression)
        {
            var objectMember = Expression.Convert(methodCallExpression, typeof(object));
            var getterLambda = Expression.Lambda<Func<object>>(objectMember);
            var getter = getterLambda.Compile();
            return getter();
        }

        throw new NotSupportedException($"Expression type {expression.GetType()} is not supported for evaluation.");
    }
}