// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;

namespace MouseWithoutBorders.Api.Controllers;

internal static class ControllerUtils
{
    public static void ValidateMachineId(string machineId)
    {
        const string allowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_";
        ArgumentNullException.ThrowIfNullOrWhiteSpace(machineId);
        machineId = machineId.Trim();
        if (string.IsNullOrEmpty(machineId))
        {
            throw new ArgumentException("Machine ID cannot be empty.", nameof(machineId));
        }

        foreach (char c in machineId)
        {
            if (!allowedChars.Contains(c))
            {
                throw new ArgumentException($"Machine ID contains an invalid character. Allowed characters are: {allowedChars}", nameof(machineId));
            }
        }
    }
}
