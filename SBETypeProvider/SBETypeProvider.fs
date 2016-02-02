namespace SBETypeProvider
open System
open System.Reflection
open Microsoft.FSharp.Core.CompilerServices
open System.IO
open Microsoft.FSharp.Quotations

//// proof of concept
//module SBETest =
//  let schema = "<type name=\"TheNumber\" primitiveType=\"uint16\" semanticType=\"TheNumber\"/>"
//  let payload = BitConverter.GetBytes(42us)
//
//  // need to get something like Assert.AreEqual(42us, SBE(schema, payload).TheNumber)


// http://www.infoq.com/articles/simple-type-providers
// method.AddXmlDocDelayed for semanticType

open ProviderImplementation.ProvidedTypes
open Microsoft.FSharp.Core.CompilerServices
open System.Reflection
open System.Xml.Linq

[<TypeProvider>]
type SbeProvider (config : TypeProviderConfig) as this =
  inherit TypeProviderForNamespaces ()

  let ns = this.GetType().Namespace
  let asm = Assembly.LoadFrom(config.RuntimeAssembly)
//  let filename = ProvidedStaticParameter("filename", typeof<string>)
//  let staticParams = [ProvidedStaticParameter("pattern", typeof<string>)]
  let tempAssembly = ProvidedAssembly( Path.ChangeExtension(Path.GetTempFileName(), ".dll"))
  let providerType  = ProvidedTypeDefinition(asm, ns, "SbeProvider", Some typeof<obj>, IsErased = false)
  do 
    tempAssembly.AddTypes [providerType]

    providerType.DefineStaticParameters(
      parameters = [ ProvidedStaticParameter("filename", typeof<string>) ], 
      instantiationFunction = (fun tyName [| :? string as path |] ->
    
      // Define the provided type, erasing to CsvFile.
      let ty = ProvidedTypeDefinition(asm, ns, tyName, Some(typeof<obj>), HideObjectMethods = true, IsErased = false)
    
      let schemaField = ProvidedField("schemaField", typeof<SbeMessageSchema>)
      ty.AddMember schemaField
     

      // Add a parameterless constructor that loads the file that was used to define the schema.
      let ctor0 = ProvidedConstructor([], 
                    InvokeCode = fun args ->
                      match args with
                      | this :: _ ->
                        Expr.FieldSet (this, schemaField, 
                          <@@ 
                            let schema = new SbeMessageSchema()
                            let xdoc = XDocument.Load(path)
                            schema.ParseXml(xdoc)
                            schema 
                          @@>)
                      | [] -> failwith "fail empty arg list"
                    )
      ty.AddMember ctor0

    // Add a constructor that takes the file name to load.
//    let ctor1 = ProvidedConstructor([ProvidedParameter("path", typeof<string>)], 
//                  InvokeCode = fun [me; path] -> 
//                    Expr.FieldSet (me, schemaField, 
//                      <@@
//                        let schema = new SbeMessageSchema()
//                        let xdoc = XDocument.Load( box (%%path) :?> string)
//                        schema.ParseXml(xdoc)
//                        schema 
//                      @@>) 
//                  )
//    ty.AddMember ctor1

    // Add a more strongly typed Data property, which uses the existing property at runtime.
      let prop = ProvidedProperty("Schema", typeof<SbeMessageSchema>, 
                                  GetterCode = fun [me] ->
                                    Expr.FieldGet (me, schemaField)
                                  )
      ty.AddMember prop

      tempAssembly.AddTypes <| [ ty ]

      ty))
    this.AddNamespace(ns, [ providerType ] )

//  let createTypes () =
//    let myType = ProvidedTypeDefinition(asm, ns, "MyType", Some typeof<obj>)
//    let myProp = ProvidedProperty("MyProperty", typeof<string>, IsStatic = true,
//                                    GetterCode = (fun args -> <@@ "Hello world"  @@>))
//    myType.AddMember(myProp)
//
//    let myType2 = ProvidedTypeDefinition(asm, ns, "MyStruct", Some typeof<ValueType>)
//
//    let myProp2 = ProvidedProperty("MyProperty2", typeof<uint16>, IsStatic = true,
//                                    GetterCode = (fun args -> <@@ 42us @@>))
//    myType2.AddMember(myProp2)
//
//    [sbeTy;myType;myType2]


