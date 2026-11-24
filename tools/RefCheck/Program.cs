// Lists the engine members (methods, constructors, fields) a built mod DLL references that a Meitou release lacks, by name and parameter
// types. Usage: RefCheck <mod.dll> <folder holding the release's Meitou*.dll>. Exit code 1 when any is missing.
//
// Why: compiling cannot catch these. A mod is built against one engine; a later release that changes a member's signature (Meitou 0.21
// gave Inventory.Add a second, optional parameter) still compiles the mod's source, but the DLL already built asks the new game for the
// old member and fails at run time with MissingMethodException. Seen in play in The Wasteland Remembers: keepsakes stopped working and the
// error took down a whole system for the session. tools/refcheck.sh runs this in CI against the newest Meitou release.
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
string mod = args[0], engineDir = args[1];
var engine = Directory.GetFiles(engineDir, "Meitou*.dll").ToList();
var resolver = new PathAssemblyResolver(Directory.GetFiles(engineDir, "*.dll").Concat(Directory.GetFiles(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "*.dll")));
using var mlc = new MetadataLoadContext(resolver);
var have = new HashSet<string>();
var types = new HashSet<string>();
foreach (var f in engine)
{
    Assembly a; try { a = mlc.LoadFromAssemblyPath(f); } catch { continue; }
    Type[] ts; try { ts = a.GetTypes(); } catch (ReflectionTypeLoadException e) { ts = e.Types.Where(t => t != null).ToArray()!; }
    foreach (var t in ts)
    {
        string tn = Clean(t.FullName ?? t.Name); types.Add(tn);
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        try {
            foreach (var m in t.GetMethods(all)) have.Add($"{tn}::{m.Name}({string.Join(",", m.GetParameters().Select(p => Name(p.ParameterType)))})");
            foreach (var c in t.GetConstructors(all)) have.Add($"{tn}::{c.Name}({string.Join(",", c.GetParameters().Select(p => Name(p.ParameterType)))})");
            foreach (var fl in t.GetFields(all)) have.Add($"{tn}::{fl.Name}");
        } catch { }
    }
}
static string Clean(string s) { int i = s.IndexOf('['); s = i >= 0 ? s[..i] : s; return s.Replace('+', '/'); }
static string Name(Type t)
{
    if (t.IsByRef) return Name(t.GetElementType()!) + "&";
    if (t.IsArray) return Name(t.GetElementType()!) + "[]";
    if (t.IsGenericParameter) return (t.DeclaringMethod != null ? "!!" : "!") + t.GenericParameterPosition;
    if (t.IsGenericType) return Clean(t.GetGenericTypeDefinition().FullName!) + "<" + string.Join(",", t.GetGenericArguments().Select(Name)) + ">";
    return Clean(t.FullName ?? t.Name);
}
using var pe = new PEReader(File.OpenRead(mod));
var md = pe.GetMetadataReader();
var prov = new Prov(md);
int checkedN = 0, bad = 0;
foreach (var h in md.MemberReferences)
{
    var r = md.GetMemberReference(h);
    string? parent = r.Parent.Kind switch
    {
        HandleKind.TypeReference => TypeName(md, (TypeReferenceHandle)r.Parent),
        HandleKind.TypeSpecification => Clean(md.GetTypeSpecification((TypeSpecificationHandle)r.Parent).DecodeSignature(prov, null)),
        _ => null,
    };
    if (parent is null || !types.Contains(parent)) continue;          // not an engine type
    string name = md.GetString(r.Name);
    string key;
    if (r.GetKind() == MemberReferenceKind.Method)
    {
        var sig = r.DecodeMethodSignature(prov, null);
        key = $"{parent}::{name}({string.Join(",", sig.ParameterTypes)})";
    }
    else key = $"{parent}::{name}";
    checkedN++;
    if (!have.Contains(key)) { bad++; Console.WriteLine("MISSING " + key); }
}
Console.WriteLine($"{checkedN} engine references checked, {bad} missing");
return bad == 0 ? 0 : 1;
static string TypeName(MetadataReader md, TypeReferenceHandle h)
{
    var t = md.GetTypeReference(h);
    string n = md.GetString(t.Name), ns = md.GetString(t.Namespace);
    if (t.ResolutionScope.Kind == HandleKind.TypeReference) return TypeName(md, (TypeReferenceHandle)t.ResolutionScope) + "/" + n;
    return Clean(ns.Length > 0 ? ns + "." + n : n);
}
sealed class Prov(MetadataReader md) : ISignatureTypeProvider<string, object?>
{
    public string GetArrayType(string e, ArrayShape s) => e + "[]";
    public string GetByReferenceType(string e) => e + "&";
    public string GetFunctionPointerType(MethodSignature<string> s) => "fnptr";
    public string GetGenericInstantiation(string g, ImmutableArray<string> a) => g + "<" + string.Join(",", a) + ">";
    public string GetGenericMethodParameter(object? c, int i) => "!!" + i;
    public string GetGenericTypeParameter(object? c, int i) => "!" + i;
    public string GetModifiedType(string m, string u, bool r) => u;
    public string GetPinnedType(string e) => e;
    public string GetPointerType(string e) => e + "*";
    public string GetPrimitiveType(PrimitiveTypeCode c) => c switch {
        PrimitiveTypeCode.Boolean => "System.Boolean", PrimitiveTypeCode.Byte => "System.Byte", PrimitiveTypeCode.Char => "System.Char",
        PrimitiveTypeCode.Double => "System.Double", PrimitiveTypeCode.Int16 => "System.Int16", PrimitiveTypeCode.Int32 => "System.Int32",
        PrimitiveTypeCode.Int64 => "System.Int64", PrimitiveTypeCode.IntPtr => "System.IntPtr", PrimitiveTypeCode.Object => "System.Object",
        PrimitiveTypeCode.SByte => "System.SByte", PrimitiveTypeCode.Single => "System.Single", PrimitiveTypeCode.String => "System.String",
        PrimitiveTypeCode.UInt16 => "System.UInt16", PrimitiveTypeCode.UInt32 => "System.UInt32", PrimitiveTypeCode.UInt64 => "System.UInt64",
        PrimitiveTypeCode.UIntPtr => "System.UIntPtr", PrimitiveTypeCode.Void => "System.Void", PrimitiveTypeCode.TypedReference => "System.TypedReference", _ => c.ToString() };
    public string GetSZArrayType(string e) => e + "[]";
    public string GetTypeFromDefinition(MetadataReader r, TypeDefinitionHandle h, byte k) { var t = r.GetTypeDefinition(h); var ns = r.GetString(t.Namespace); return (ns.Length > 0 ? ns + "." : "") + r.GetString(t.Name); }
    public string GetTypeFromReference(MetadataReader r, TypeReferenceHandle h, byte k) { var t = r.GetTypeReference(h); string n = r.GetString(t.Name); if (t.ResolutionScope.Kind == HandleKind.TypeReference) return GetTypeFromReference(r, (TypeReferenceHandle)t.ResolutionScope, k) + "/" + n; var ns = r.GetString(t.Namespace); return ns.Length > 0 ? ns + "." + n : n; }
    public string GetTypeFromSpecification(MetadataReader r, object? c, TypeSpecificationHandle h, byte k) => r.GetTypeSpecification(h).DecodeSignature(this, c);
}
