# Key autocompletion
[Meteion.Toolkit.Localization.KeysGenerator](https://www.nuget.org/packages/Meteion.Toolkit.Localization.KeysGenerator) is a Roslyn source generator that turns every key in your neutral `.resx` files into a `public const string`, so both code-behind and XAML get autocompletion over your resource keys - and a rename or typo becomes a compile error instead of a silent runtime miss.

Add it to your app project:
```bash
dotnet add package Meteion.Toolkit.Localization.KeysGenerator
```
That's it - it wires itself into your build automatically, no other setup required. For `Properties\Strings.resx` in `MyApp`, it generates a class named `StringsKeys` (in a namespace mirroring the resx's folder, same convention the .NET SDK's own resx code generator uses) with a `ResxSource` constant plus one qualified-key constant per resx key, with the resx value as that const's XML-doc summary:

```csharp
namespace MyApp.Properties
{
    public static partial class StringsKeys
    {
        /// <summary>
        /// Identifies this resx (<c>&lt;AssemblyName&gt;/&lt;ResourceBaseName&gt;</c>) - for <c>LocalizedValueExtension.Source</c>.
        /// </summary>
        public const string ResxSource = "MyApp/MyApp.Properties.Strings";

        /// <summary>
        /// <c>"Hello there!"</c>
        /// </summary>
        public const string Greeting = "MyApp/MyApp.Properties.Strings:Greeting";
    }
}
```

Use it in code-behind exactly like any other const:
```csharp
var text = loc.GetString(StringsKeys.Greeting);
```

In XAML, reference it via `x:Static` for full IntelliSense and a compile-time-checked key:
```xml
<TextBlock Text="{lx:LocalizedValue {x:Static strings:StringsKeys.Greeting}}" />
```

You can also just type `Key="..."` as a plain string literal - `LocalizedValueExtension.Key` carries a `TypeConverter` that lists every qualified key discovered from a generated class in any loaded assembly, so Visual Studio's XAML editor offers it as an attribute-value dropdown. This is a convenience list, not a validator (it can't see a key from a different, not-yet-loaded assembly, or one that's only ever supplied via `KeyBinding`), so prefer `x:Static` when you want the key checked at compile time.

If a class/namespace name would collide with something else (e.g. the standard `ResXFileCodeGenerator`'s own `Strings.Designer.cs`, which the generator's default `{ResxBaseName}Keys` naming is deliberately chosen to avoid), override either per file. If the resx's manifest resource name isn't the MSBuild default (e.g. it has `LogicalName` or `DependentUpon` set), override the resource base name baked into its keys too:
```xml
<AdditionalFiles Update="Properties\Strings.resx" MeteionKeysClassName="Text" MeteionKeysNamespace="MyApp.Localization" />
<AdditionalFiles Update="Legacy\Old.resx" MeteionResourceBaseName="MyApp.LegacyStrings" />
```

A resx key literally named `ResxSource` collides with the generated constant of the same name: its constant is generated as `ResxSource_2` instead, with warning `MTKGEN001`.

If you'd rather not ship the generator inside your app at all (it's a build-time-only analyzer, never a runtime dependency - `dotnet add package` already sets this up correctly), reference it as:
```
<PackageReference Include="Meteion.Toolkit.Localization.KeysGenerator" PrivateAssets="all" />
```
