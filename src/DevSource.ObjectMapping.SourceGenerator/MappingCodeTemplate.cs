namespace DevSource.ObjectMapping.SourceGenerator;

internal readonly record struct MappingCodeTemplate(
    string NamespaceName,
    string SourceName,
    string TargetName,
    string PropertyAssignments,
    string CollectionHelpers,
    string DictionaryHelpers,
    string OnBeforeMapCall,
    string OnAfterMapCall,
    bool HasEmptyBody);
