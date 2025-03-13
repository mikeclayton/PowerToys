// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.AspNetCore.Mvc.Controllers;

namespace MouseWithoutBorders.Api.Server;

public sealed class CustomControllerFeatureProvider : ControllerFeatureProvider
{
    public CustomControllerFeatureProvider(IEnumerable<Type> controllerTypes)
    {
        this.ControllerTypes = new(controllerTypes ?? throw new ArgumentNullException(nameof(controllerTypes)));
    }

    private HashSet<Type> ControllerTypes
    {
        get;
    }

    protected override bool IsController(TypeInfo typeInfo)
    {
        return this.ControllerTypes.Contains(typeInfo.AsType());
    }
}
