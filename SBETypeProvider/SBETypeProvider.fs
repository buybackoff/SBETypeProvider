namespace SBETypeProvider
open System
open System.Reflection
open Microsoft.FSharp.Core.CompilerServices
open System.IO
open Microsoft.FSharp.Quotations

open Spreads
open Spreads.Serialization

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
    // http://stackoverflow.com/a/10365702/801189
    System.AppDomain.CurrentDomain.add_AssemblyResolve(fun _ args ->
        let name = System.Reflection.AssemblyName(args.Name)
        let existingAssembly = 
            System.AppDomain.CurrentDomain.GetAssemblies()
            |> Seq.tryFind(fun a -> System.Reflection.AssemblyName.ReferenceMatchesDefinition(name, a.GetName()))
        match existingAssembly with
        | Some a -> a
        | None -> null
    )

    tempAssembly.AddTypes [providerType]

    providerType.DefineStaticParameters(
      parameters = [ ProvidedStaticParameter("filename", typeof<string>) ], 
      instantiationFunction = (fun tyName [| :? string as path |] ->
    
      // Define the provided type, erasing to CsvFile.
      let ty = ProvidedTypeDefinition(asm, ns, tyName, Some(typeof<obj>), HideObjectMethods = true, IsErased = false)
    
      
      let internalSchema = new SbeMessageSchema()
      let xdoc = XDocument.Load(path)
      internalSchema.ParseXml(xdoc)

      let schemaField = ProvidedField("_schema", typeof<SbeMessageSchema>)
      ty.AddMember schemaField
     
       // Add a parameterless constructor that loads the file that was used to define the schema.
      let ctor0 = 
        ProvidedConstructor([], 
          InvokeCode = fun args ->
            match args with
            | this :: _  ->
              Expr.FieldSet (this, schemaField, 
                <@@ 
                  let schema = new SbeMessageSchema()
                  let xdoc = XDocument.Load(path)
                  schema.ParseXml(xdoc)
                  schema
                @@>)
            | [] -> failwith "empty arg list"
        )
      ty.AddMember ctor0

      let prop = ProvidedProperty("Schema", typeof<SbeMessageSchema>, 
                                  GetterCode = fun [me] ->
                                    Expr.FieldGet (me, schemaField)
                                  )
      ty.AddMember prop


      // generate a type for each message
      // here we basically unpack the message elements we have packed in SbeMessageSchema
      let generateMessages () =
        for message in internalSchema.Messages do
          // Message type
          let mTy = ProvidedTypeDefinition(asm, ns, message.Key, Some(typeof<ValueType>), 
                        HideObjectMethods = true, IsErased = false)
          // Direct buffer
          let bufferField = ProvidedField("_directBuffer", typeof<IDirectBuffer>)
          mTy.AddMember bufferField
          
          let mCtor1 = 
            ProvidedConstructor(
              [ProvidedParameter("buffer", typeof<IDirectBuffer>)], 
              InvokeCode = fun args ->
                match args with
                | [this;buffer] ->
                  Expr.FieldSet (this, bufferField, <@@ %%buffer:IDirectBuffer @@>)
                | _ -> failwith "wrong ctor params"
            )
          mTy.AddMember mCtor1

          // Direct Buffer property
          let mBufferProp = 
            ProvidedProperty("DirectBuffer", typeof<IDirectBuffer>, 
                GetterCode = (fun [this] ->
                  Expr.FieldGet (this, bufferField)
                ),
                SetterCode = (fun [this;buffer] ->
                    Expr.FieldSet (this, bufferField, <@@ %%buffer:IDirectBuffer @@>)
                )
            )
          mTy.AddMember mBufferProp
          
          

          let mMethod = ProvidedMethod(message.Key, [ProvidedParameter("buffer", typeof<IDirectBuffer>)], mTy)
          mMethod.InvokeCode <- (fun [this;buffer] ->
              <@@ 
                let c = mTy.GetConstructor([| typeof<IDirectBuffer>|]) 
                c.Invoke([| box (%%buffer:IDirectBuffer) |])
              @@>
            )

          ty.AddMember mMethod

          tempAssembly.AddTypes <| [ mTy ]
          
          // 2. for each message type, generate properties
          // 3. complex propeties are types
          ()
      
      generateMessages ()

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


