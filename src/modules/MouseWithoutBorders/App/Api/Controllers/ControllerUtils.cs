// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using MouseWithoutBorders.Api.Models;
using MouseWithoutBorders.Core;

namespace MouseWithoutBorders.Api.Controllers;

internal static class ControllerUtils
{
    /// <summary>
    /// TODO: implement a better encryption mechanism.
    /// </summary>
    public static string EncryptString(string plainText, string privateKey)
    {
        // generate a new IV for each encryption
        using var aes = Aes.Create();

        // generate the private key bytes
        aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(privateKey));

        // generate a random public key and write it to the result
        // so we can read it to decrypt the rest of the stream
        using var memoryStream = new MemoryStream();
        aes.GenerateIV();
        memoryStream.Write(aes.IV, 0, aes.IV.Length);

        // write the encrypted security key
        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
        using var writer = new StreamWriter(cryptoStream);
        writer.Write(plainText);
        writer.Flush();
        cryptoStream.FlushFinalBlock();

        var encrypted = memoryStream.ToArray();
        var base64 = Convert.ToBase64String(encrypted);
        return base64;
    }

    /// <summary>
    /// TODO: implement a better encryption mechanism.
    /// </summary>
    public static string DecryptString(string encryptedValue, string privateKey)
    {
        using var aes = Aes.Create();

        var buffer = Convert.FromBase64String(encryptedValue);
        using var memoryStream = new MemoryStream(buffer);

        // extract IV from the ciphertext
        var iv = new byte[aes.IV.Length];
        memoryStream.Read(iv, 0, iv.Length);
        aes.IV = iv;

        aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(privateKey));

        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
        using var reader = new StreamReader(cryptoStream);
        var plainText = reader.ReadToEnd();

        return plainText;
    }

    /// <summary>
    /// TODO: implement a better encryption mechanism.
    /// </summary>
    public static bool TryValidateSecurityKey(string encryptedKey, [NotNullWhen(false)] out ActionResult? response)
    {
        // try to decrypt the security key to get the encrypted timestamp
        var plainText = default(string);
        try
        {
            plainText = ControllerUtils.DecryptString(encryptedKey, Common.MyKey);
        }
        catch
        {
            response = new ObjectResult(
                new { Message = $"Invalid security key." })
            {
                StatusCode = (int)HttpStatusCode.Forbidden,
            };
            return false;
        }

        // read the timestamp from the key
#if DEBUG
        var timeout = TimeSpan.FromMinutes(60);
#else
        var timeout = TimeSpan.FromSeconds(10);
#endif
        var timestamp = new DateTime(long.Parse(plainText, CultureInfo.InvariantCulture));
        if ((timestamp + timeout) < DateTime.UtcNow)
        {
            response = new ObjectResult(
                new { Message = $"The security key has expired." })
            {
                StatusCode = (int)HttpStatusCode.Forbidden,
            };
            return false;
        }

        response = null;
        return true;
    }

    public static bool TryValidateMachineId(string machineId, [NotNullWhen(false)] out ActionResult? response)
    {
        const string allowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_";
        ArgumentNullException.ThrowIfNullOrWhiteSpace(machineId);
        machineId = machineId.Trim();
        if (string.IsNullOrEmpty(machineId))
        {
            response = new BadRequestObjectResult(
                new { Message = "Machine ID cannot be empty." });
            return false;
        }

        foreach (char c in machineId)
        {
            if (!allowedChars.Contains(c))
            {
                response = new BadRequestObjectResult(
                    new { Message = $"Machine ID contains invalid characters." });
                return false;
            }
        }

        response = null;
        return true;
    }

    public static bool TryValidateMatrixId(string machineId, [NotNullWhen(false)] out ActionResult? response)
    {
        var stringComparer = StringComparison.OrdinalIgnoreCase;
        var matrixId = MachineStuff.MachineMatrix
            .Where(matrixId => !string.IsNullOrEmpty(matrixId))
            .FirstOrDefault(matrixId => string.Equals(matrixId, machineId, stringComparer));
        if (matrixId is null)
        {
            response = new NotFoundObjectResult(
                new { Message = "The specified machine was not found." });
            return false;
        }

        response = null;
        return true;
    }

    public static bool IsLocalMachineId(string machineId)
    {
        var stringComparer = StringComparison.OrdinalIgnoreCase;
        return string.Equals(machineId, Environment.MachineName, stringComparer);
    }

    public static List<ScreenInfo> GetLocalScreens()
    {
        var localMonitorInfo = new List<Class.NativeMethods.MonitorInfoEx>();

        bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref Class.NativeMethods.RECT lprcMonitor, IntPtr dwData)
        {
            var mi = default(Class.NativeMethods.MonitorInfoEx);
            mi.cbSize = Marshal.SizeOf(mi);
            _ = Class.NativeMethods.GetMonitorInfo(hMonitor, ref mi);
            localMonitorInfo.Add(mi);
            return true;
        }

        Class.NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorEnumProc, IntPtr.Zero);

        const int MONITORINFOF_PRIMARY = 0x00000001;
        var screenInfo = localMonitorInfo.Select(
            (monitorInfo, index) => new ScreenInfo(
                id: index,
                primary: (monitorInfo.dwFlags & MONITORINFOF_PRIMARY) == MONITORINFOF_PRIMARY,
                displayArea: new(
                    x: monitorInfo.rcMonitor.Left,
                    y: monitorInfo.rcMonitor.Top,
                    width: monitorInfo.rcMonitor.Right - monitorInfo.rcMonitor.Left,
                    height: monitorInfo.rcMonitor.Bottom - monitorInfo.rcMonitor.Top),
                workingArea: new(
                    x: monitorInfo.rcWork.Left,
                    y: monitorInfo.rcWork.Top,
                    width: monitorInfo.rcWork.Right - monitorInfo.rcWork.Left,
                    height: monitorInfo.rcWork.Bottom - monitorInfo.rcWork.Top)))
            .ToList();

        return screenInfo;
    }
}
