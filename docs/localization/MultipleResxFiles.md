# Multiple resx files

An assembly can hold any number of resx files - e.g. one per feature folder, rather than one huge
`Resources.resx` full of `Category_Page_Thing` keys. Every key then has to say which resx it's in, so
keys come in two forms:

| Form | Example | Resolves from |
| --- | --- | --- |
| Qualified | `MyApp/MyApp.Properties.Strings:Greeting` | The named assembly and resx |
| Unqualified | `Greeting` | `Source`, or else the assembly's only resx |

A qualified key is `<AssemblyName>/<ResourceBaseName>:<Key>`, where the resource base name is the
resx's manifest resource name (what `ResourceManager` takes) - `RootNamespace` + folder + file name by
default. You'll rarely type one: the generator's constants hold qualified keys, and each generated
class's `ResxSource` constant holds the `<AssemblyName>/<ResourceBaseName>` part for `Source`.

The rules `LocalizedValue` applies, to a literal `Key` and to every value a `KeyBinding` produces:

1. `Source` and `Assembly` can't both be set - `Source` already names the assembly.
2. A qualified key can't be combined with `KeyPrefix`.
3. A qualified key must agree with `Source`/`Assembly` when either is set.
4. An unqualified key + `Source` resolves `Source:KeyPrefix+key`.
5. An unqualified key without `Source` resolves against the assembly (`Assembly`, else the XAML's own
   assembly, else `LocalizationOptions.DefaultAssembly`)'s only resx - and throws if it has several.

A literal `Key` that breaks a rule throws `LocalizationConfigurationException`. A bound value that
breaks one is always reported in Visual Studio's XAML Binding Failures output, then handled like a
missing key according to `LocalizationOptions.MissingKeyBehavior`: `ThrowException` rethrows the
`LocalizationConfigurationException`, `ReturnKey` displays the raw value, and `ReturnEmptyString`
displays nothing.

Because a qualified key carries its assembly, keys from a referenced library just work:
`{lx:LocalizedValue {x:Static shared:CommonKeys.Ok}}` resolves from the library's resx even from your
app's XAML, with no `Assembly=` needed.
