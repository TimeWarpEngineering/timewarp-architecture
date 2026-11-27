#region Purpose
// Endpoint metadata that names and summarizes a hand-written operation in the OpenAPI document.
#endregion

#region Design
// A contract request carries its [Operation] as generated OperationName and
// OperationDescription constants, and OpenApiOperationNames reads those. An endpoint with no
// contract request (EndpointWithoutRequest) has nothing to read, so it adds this record with
// Description(builder => builder.WithMetadata(...)). The record wins over the request type.
#endregion

namespace TimeWarp.Foundation;

/// <summary>
/// Endpoint metadata naming an operation whose request type carries no generated
/// <c>OperationName</c> constant (for example an <c>EndpointWithoutRequest</c>).
/// </summary>
/// <param name="Name">The operation id, <c>&lt;resource&gt;.&lt;action&gt;</c>.</param>
/// <param name="Description">One sentence. It becomes the OpenAPI summary.</param>
public sealed record OpenApiOperationName(string Name, string Description);
