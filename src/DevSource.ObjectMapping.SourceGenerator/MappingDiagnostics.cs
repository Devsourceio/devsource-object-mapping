using Microsoft.CodeAnalysis;

namespace DevSource.ObjectMapping.SourceGenerator;

/// <summary>
/// Provides a collection of diagnostic descriptors for common issues encountered
/// during object-to-object mapping operations. The diagnostics are used to
/// identify potential mapping errors, warnings, and unsupported scenarios within
/// the source generation process.
/// </summary>
public static class MappingDiagnostics
{
  private const string Category = "DevSource.ObjectMapping";

  /// <summary>
  /// Represents a diagnostic descriptor for detecting cases where a property exists in the source object
  /// but is missing in the target object during mapping operations.
  /// </summary>
  /// <remarks>
  /// This diagnostic is triggered when the mapping source contains a property that does not have a
  /// corresponding property in the mapping target. It helps in identifying incomplete mappings that
  /// could potentially lead to data loss or inconsistencies.
  /// </remarks>
  /// <example>
  /// Diagnostic ID: DSM001
  /// Severity: Error
  /// Message Format: "Property '{0}' exists in source '{1}' but not in target '{2}'"
  /// </example>
  public static readonly DiagnosticDescriptor DSM001 = new(
      id: "DSM001",
      title: "Missing Target Property",
      messageFormat: "Property '{0}' exists in source '{1}' but not in target '{2}'",
      category: Category,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true);

  /// <summary>
  /// Represents a diagnostic descriptor for identifying type mismatches during object mapping operations.
  /// </summary>
  /// <remarks>
  /// This diagnostic is triggered when a property in the source object cannot be mapped to a property in the target object
  /// due to type incompatibility. It ensures that potential runtime errors or logical issues arising from
  /// mismatched property types are caught at compile time.
  /// </remarks>
  /// <example>
  /// Diagnostic ID: DSM002
  /// Severity: Error
  /// Message Format: "Cannot map property '{0}' from type '{1}' to '{2}'"
  /// </example>
  public static readonly DiagnosticDescriptor DSM002 = new(
      id: "DSM002",
      title: "Type Mismatch",
      messageFormat: "Cannot map property '{0}' from type '{1}' to '{2}'",
      category: Category,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true);

  /// <summary>
  /// Represents a diagnostic descriptor for detecting cases where a nested type mapping is missing
  /// during object mapping operations.
  /// </summary>
  /// <remarks>
  /// This diagnostic is triggered when a property refers to a complex or nested type, and no specific
  /// mapping logic is provided for that type. To resolve this issue, the developer should implement
  /// the necessary mapping logic, such as implementing the `IMapTo<T>` interface for the nested type.
  /// </remarks>
  /// <example>
  /// Diagnostic ID: DSM003
  /// Severity: Error
  /// Message Format: "No mapping found for nested type '{0}'. Implement IMapTo<{0}> on the element type for proper mapping of nested objects."
  /// </example>
  public static readonly DiagnosticDescriptor DSM003 = new(
      id: "DSM003",
      title: "Missing Nested Mapping",
      messageFormat: "No mapping found for nested type '{0}'. Implement IMapTo<{0}> on the element type for proper mapping of nested objects.",
      category: Category,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true);

  /// <summary>
  /// Represents a diagnostic descriptor for identifying cases where a nullable source property
  /// is being mapped to a non-nullable target property during mapping operations.
  /// </summary>
  /// <remarks>
  /// This diagnostic is triggered when there is a mismatch between the nullability of the source
  /// and target properties in a mapping configuration. It ensures that potential runtime exceptions
  /// caused by assigning null values to non-nullable properties are addressed during development.
  /// </remarks>
  /// <example>
  /// Diagnostic ID: DSM004
  /// Severity: Warning
  /// Message Format: "Nullable mismatch: '{0}' may be null when assigned to non-nullable '{1}'. Make the target nullable or handle the assignment explicitly."
  /// </example>
  public static readonly DiagnosticDescriptor DSM004 = new(
      id: "DSM004",
      title: "Nullable Mismatch",
      messageFormat: "Nullable mismatch: '{0}' may be null when assigned to non-nullable '{1}'. Make the target nullable or handle the assignment explicitly.",
      category: Category,
      defaultSeverity: DiagnosticSeverity.Warning,
      isEnabledByDefault: true);

  /// <summary>
  /// Represents a diagnostic descriptor for identifying attempts to map unsupported types during mapping operations.
  /// </summary>
  /// <remarks>
  /// This diagnostic is triggered when a source or target property involves a type that is not supported by the
  /// mapping generator. This helps to ensure that mappings are only attempted for compatible and supported types,
  /// avoiding runtime errors or incorrect behaviors.
  /// </remarks>
  /// <example>
  /// Diagnostic ID: DSM005
  /// Severity: Error
  /// Message Format: "Type '{0}' is not supported for mapping"
  /// </example>
  public static readonly DiagnosticDescriptor DSM005 = new(
      id: "DSM005",
      title: "Unsupported Type",
      messageFormat: "Type '{0}' is not supported for mapping",
      category: Category,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true);

  /// <summary>
  /// Represents a diagnostic descriptor for identifying cases where a property in the target object
  /// is read-only and cannot be assigned a value during mapping operations.
  /// </summary>
  /// <remarks>
  /// This diagnostic is triggered when the mapping source attempts to set a property in the target
  /// object, but the target property lacks a setter. It helps in detecting potential runtime issues
  /// where the mapping cannot complete due to immutability of the target property.
  /// </remarks>
  /// <example>
  /// Diagnostic ID: DSM006
  /// Severity: Error
  /// Message Format: "Property '{0}' in '{1}' does not have a setter"
  /// </example>
  public static readonly DiagnosticDescriptor DSM006 = new(
      id: "DSM006",
      title: "Readonly Target Property",
      messageFormat: "Property '{0}' in '{1}' does not have a setter",
      category: Category,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true);

  /// <summary>
  /// Represents a diagnostic descriptor for identifying ambiguous mapping scenarios during object mapping operations.
  /// </summary>
  /// <remarks>
  /// This diagnostic is triggered when multiple potential mapping candidates are detected for a specific source element,
  /// causing ambiguity. It emphasizes the need to explicitly specify the target type to resolve the ambiguity and ensure
  /// accurate mapping.
  /// </remarks>
  /// <example>
  /// Diagnostic ID: DSM007
  /// Severity: Error
  /// Message Format: "Ambiguous mapping detected for '{0}'. Multiple mapping candidates found. Specify the target type explicitly."
  /// </example>
  public static readonly DiagnosticDescriptor DSM007 = new(
      id: "DSM007",
      title: "Ambiguous Mapping",
      messageFormat: "Ambiguous mapping detected for '{0}'. Multiple mapping candidates found. Specify the target type explicitly.",
      category: Category,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true);

  /// <summary>
  /// Represents a diagnostic descriptor for identifying missing mappings for collection element types during object mapping operations.
  /// </summary>
  /// <remarks>
  /// This diagnostic is triggered when a collection type is being mapped, but no mapping exists for its element type.
  /// This aids in detecting incomplete or unsupported mappings for collections, ensuring consistency and correctness in data transformation processes.
  /// </remarks>
  /// <example>
  /// Diagnostic ID: DSM008
  /// Severity: Error
  /// Message Format: "No mapping found for collection element type '{0}'"
  /// </example>
  public static readonly DiagnosticDescriptor DSM008 = new(
      id: "DSM008",
      title: "Collection Element Mapping Missing",
      messageFormat: "No mapping found for collection element type '{0}'",
      category: Category,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true);

  /// <summary>
  /// Represents a diagnostic descriptor for detecting circular references
  /// during mapping operations.
  /// </summary>
  /// <remarks>
  /// This diagnostic is triggered when a circular reference is identified
  /// within the mapping process. Circular references can lead to infinite
  /// recursions or stack overflow errors if not handled appropriately.
  /// The diagnostic helps in identifying such problematic references.
  /// </remarks>
  /// <example>
  /// Diagnostic ID: DSM009
  /// Severity: Warning
  /// Message Format: "Circular reference detected involving type '{0}'"
  /// </example>
  public static readonly DiagnosticDescriptor DSM009 = new(
      id: "DSM009",
      title: "Circular Reference Detected",
      messageFormat: "Circular reference detected involving type '{0}'",
      category: Category,
      defaultSeverity: DiagnosticSeverity.Warning,
      isEnabledByDefault: true);

  /// <summary>
  /// Represents a diagnostic descriptor for detecting cases where a compatible source collection
  /// is missing during a root collection mapping operation.
  /// </summary>
  /// <remarks>
  /// This diagnostic is triggered when a mapping target expects a root collection to map from a source,
  /// but no compatible collection is found in the source type. It helps identify issues in collection-based
  /// mappings where the source does not provide the necessary data structure to map to the target.
  /// </remarks>
  /// <example>
  /// Diagnostic ID: DSM011
  /// Severity: Error
  /// Message Format: "No compatible source collection found on '{0}' for root target '{1}'"
  /// </example>
  public static readonly DiagnosticDescriptor DSM011 = new(
      id: "DSM011",
      title: "Root Collection Source Missing",
      messageFormat: "No compatible source collection found on '{0}' for root target '{1}'",
      category: Category,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true);

  /// <summary>
  /// Represents a diagnostic descriptor for identifying ambiguous root collection sources
  /// during mapping operations.
  /// </summary>
  /// <remarks>
  /// This diagnostic is triggered when multiple compatible source collections are found
  /// for a single root target object. It highlights potential confusion in determining
  /// the correct source collection to map from, ensuring accuracy and preventing unintended
  /// behavior in the mapping configuration.
  /// </remarks>
  /// <example>
  /// Diagnostic ID: DSM012
  /// Severity: Error
  /// Message Format: "Multiple compatible source collections found on '{0}' for root target '{1}'"
  /// </example>
  public static readonly DiagnosticDescriptor DSM012 = new(
      id: "DSM012",
      title: "Ambiguous Root Collection Source",
      messageFormat: "Multiple compatible source collections found on '{0}' for root target '{1}'",
      category: Category,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true);

  /// <summary>
  /// Represents a diagnostic descriptor for detecting cases where an element in the source collection
  /// lacks a corresponding mapping to an element in the target collection during root collection mapping operations.
  /// </summary>
  /// <remarks>
  /// This diagnostic is triggered when a root collection mapping is attempted, but no valid mapping is found
  /// between one or more elements of the source collection and those of the target collection.
  /// It helps identify incomplete or missing collection element mappings that could cause runtime issues
  /// or inconsistent data transformations.
  /// </remarks>
  /// <example>
  /// Diagnostic ID: DSM013
  /// Severity: Error
  /// Message Format: "No mapping found from source collection element '{0}' to target element '{1}' for root collection mapping"
  /// </example>
  public static readonly DiagnosticDescriptor DSM013 = new(
      id: "DSM013",
      title: "Root Collection Element Mapping Missing",
      messageFormat: "No mapping found from source collection element '{0}' to target element '{1}' for root collection mapping",
      category: Category,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true);
}
