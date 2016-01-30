namespace SBETypeProvider
open System

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

[<TypeProvider>]
type SBEProvider (config : TypeProviderConfig) as this =
  inherit TypeProviderForNamespaces ()

  let ns = "SBETypeProvider.Provided"
  let asm = Assembly.GetExecutingAssembly()

  let createTypes () =
    let myType = ProvidedTypeDefinition(asm, ns, "MyType", Some typeof<obj>)
    let myProp = ProvidedProperty("MyProperty", typeof<string>, IsStatic = true,
                                    GetterCode = (fun args -> <@@ "Hello world" @@>))
    myType.AddMember(myProp)

    let myType2 = ProvidedTypeDefinition(asm, ns, "MyStruct", Some typeof<ValueType>)
    let myProp2 = ProvidedProperty("MyProperty2", typeof<uint16>, IsStatic = true,
                                    GetterCode = (fun args -> <@@ 42us @@>))
    myType2.AddMember(myProp2)

    [myType;myType2]

  do
    this.AddNamespace(ns, createTypes())
