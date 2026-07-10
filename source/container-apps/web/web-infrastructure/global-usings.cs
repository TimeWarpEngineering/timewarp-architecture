#region Purpose
// Project-wide using directives so individual files omit repeated imports.
#endregion

global using FluentValidation;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.Metadata.Builders;
global using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
// Solution usings
global using TimeWarp.Architecture;
global using TimeWarp.Foundation;
#if(postgres)
global using TimeWarp.Architecture.Configuration;
#endif
global using TimeWarp.Foundation.Configuration;
global using TimeWarp.Architecture.Entities;
global using TimeWarp.Foundation.Entities;
global using TimeWarp.Mediator;
global using TimeWarp.Modules;
