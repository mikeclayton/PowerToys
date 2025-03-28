// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace MouseWithoutBorders.Api.Models.Messages;

public struct MachineMatrixResponse
{
    [JsonInclude]
    public List<string> Matrix;

    public MachineMatrixResponse(List<string> matrix)
    {
        this.Matrix = matrix ?? throw new ArgumentNullException(nameof(matrix));
    }
}
