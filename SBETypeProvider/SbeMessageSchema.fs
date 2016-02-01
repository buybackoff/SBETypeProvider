namespace SBETypeProvider

open System
open System.Linq
open System.Xml.Linq
open System.Collections.Generic

open Spreads

[<AutoOpenAttribute>]
module XmlHelper =
  let car = XDocument.Load(__SOURCE_DIRECTORY__ + "/Car.xml")
  let fix = XDocument.Load(__SOURCE_DIRECTORY__ + "/FIX.xml")
  let xn (s:string) = XName.op_Implicit(s)
  let ln (xe:XElement) = xe.Name.LocalName 
  let xattr (xe:XElement) (name:string)  = try xe.Attribute(xn name).Value with | _ -> ""
  let xattrd (xe:XElement) (name:string) (dflt:string)  = try xe.Attribute(xn name).Value with | _ -> dflt
  let xelem (xe:XElement) (name:string)  = try xe.Element(xn name).Value with | _ -> ""

type SbeTypeName = string
type SbeMessageName = string
type SbeElementName = string
type SbeOffset = int
type SbeVersion = int
type SbePresence =
  | Required = 0
  | Constant = 1
  | Optional = 2


type SchemaElement() =
  /// Name of the message.
  member val Name : SbeElementName = "" with get, set
  /// Unique ID of a message template.
  member val Id = 0 with get, set
  /// Description of the message. (optional)
  member val Description : string = "" with get, set
  ///  The semantic type of the message.
  member val SemanticType : string = "" with get, set

  



type SbeType() =
  inherit SchemaElement()
  
type PrimitiveType() =
  inherit SbeType()
  member val Type = Unchecked.defaultof<Type> with get, set
  member val Size = 0 with get, set

type SimpleType() =
  inherit SbeType()
  member val PrimitiveType = Unchecked.defaultof<PrimitiveType> with get, set
  member val Length = 1 with get, set
  member val Presence = SbePresence.Required with get, set
  member val CharacterEncoding = "" with get, set
  member val DefaultValue = "" with get, set

type CompositeType() =
  inherit SbeType()
  member val InnerTypes = new List<SimpleType>() with get

type EnumType() =
  inherit SbeType()
  member val Presence = SbePresence.Required with get, set
  member val EncodingType = Unchecked.defaultof<PrimitiveType> with get, set

// TODO enum + set

//[<RequireQualifiedAccessAttribute>]
//type SbeTypeX = 
//| Primitive of PrimitiveType
//| Composite of SbeType seq
//| GroupDimension of numInGroup:PrimitiveType * blockSize:PrimitiveType
//| VarData of length:PrimitiveType * varData:PrimitiveType



type MessageElement() =
  inherit SchemaElement()

type Field() =
  inherit MessageElement()
  // we calculate it even when it is not explicitly provided
  member val Offset = 0 with get, set
  member val Size = 0 with get, set
  member val Type : SbeType = Unchecked.defaultof<_> with get, set
  member val SinceVersion : SbeVersion = 0 with get, set
  member val CompositeFields = new List<Field>() with get, set

type Group() =
  inherit MessageElement()
  member val DimensionType : SbeType = Unchecked.defaultof<_> with get, set
  member val BlockLength : int = 0 with get, set
  member val GroupElements = new List<MessageElement>() with get
  member val VarDataType : CompositeType = Unchecked.defaultof<_> with get, set

type Data() =
  inherit MessageElement()
  member val Type : SbeType = Unchecked.defaultof<_> with get, set
  member val SinceVersion : SbeVersion = 0 with get, set

type SbeMessage() =
  inherit SchemaElement()
  member val BlockLength : int = 0 with get, set
  member val MessageElements = List<MessageElement>() with get

  
type SbeMessageSchema () =
  inherit SchemaElement()

  static let primitives = new Dictionary<SbeTypeName,PrimitiveType>()
  static do
    primitives.Add("char", new PrimitiveType(Type = typeof<sbyte>, Size = sizeof<sbyte>))
    primitives.Add("int8", new PrimitiveType(Type = typeof<int8>, Size = sizeof<int8>))
    primitives.Add("int16", new PrimitiveType(Type = typeof<int16>, Size = sizeof<int16>))
    primitives.Add("int32", new PrimitiveType(Type = typeof<int32>, Size = sizeof<int32>))
    primitives.Add("int64", new PrimitiveType(Type = typeof<int64>, Size = sizeof<int64>))
    primitives.Add("uint8", new PrimitiveType(Type = typeof<uint8>, Size = sizeof<uint8>))
    primitives.Add("uint16", new PrimitiveType(Type = typeof<uint16>, Size = sizeof<uint16>))
    primitives.Add("uint32", new PrimitiveType(Type = typeof<uint32>, Size = sizeof<uint32>))
    primitives.Add("uint64", new PrimitiveType(Type = typeof<uint64>, Size = sizeof<uint64>))
    primitives.Add("float", new PrimitiveType(Type = typeof<float32>, Size = sizeof<float32>))
    primitives.Add("double", new PrimitiveType(Type = typeof<double>, Size = sizeof<double>))
  static member PrimitiveTypes with get() = primitives :> IReadOnlyDictionary<SbeTypeName,PrimitiveType>

  member val Package = "" with get, set
  member val Version: SbeVersion = 0 with get, set
  member val SemanticVersion: string = "" with get, set
  member val ByteOrder: string = "littleEndian" with get, set
  
  member val Types = Dictionary<SbeTypeName, SbeType>() with get
  member val Messages = Dictionary<SbeMessageName, SbeMessage>() with get

  member public this.ParseXml(document:XDocument) =
    let rec parseAux (xe:XElement) =
      match ln xe with
      | "messageSchema" -> 
        this.Package <- xattr xe "package"
        //this.Id <- int <| xattrd xe "id" "0"
        this.Version <- int <| xattrd xe "version" "0"
        this.SemanticVersion <- xattr xe "semanticVersion"
        this.Description <- xattr xe "description"
        this.ByteOrder <- xattrd xe "byteOrder" "littleEndian"
        parseAux (xe.Elements().First())
      | "types" -> 
        parseTypes xe (xe.Elements())
      | "message" -> 
        parseMessage xe
      | _ as x ->
        Console.WriteLine("Unknowun element " + x)
    and parseTypes (parent) (xes:XElement seq) =
      let parseSimpleType xe = 
        let ty = new SimpleType()
        ty.Name <- xattr xe "name"
        ty.Presence <- Enum.Parse(typeof<SbePresence>, (xattrd xe "presence" "required"), true) :?> SbePresence
        ty.SemanticType <- xattr xe "semanticType"
        ty.Description <- xattr xe "description"
        ty.PrimitiveType <- SbeMessageSchema.PrimitiveTypes.[(xattr xe "primitiveType")]
        ty.Length <- int <| (xattrd xe "length" "0")
        ty.CharacterEncoding <- xattr xe "characterEncoding"
        ty.DefaultValue <- xe.Value
        ty
      for xe in xes do
        match xe.Name.LocalName with
        | "type" ->
          let ty = parseSimpleType xe
          this.Types.Add(ty.Name, ty)
          Console.WriteLine("Parsing Simple Type " + ty.Name )
        | "composite" ->
          let ty = new CompositeType()
          ty.Name <- xattr xe "name"
          ty.SemanticType <- xattr xe "semanticType"
          ty.Description <- xattr xe "description"
          for xe' in xe.Elements() do
            let ty' = parseSimpleType xe'
            ty.InnerTypes.Add(ty')
          this.Types.Add(ty.Name, ty)
          Console.WriteLine("Parsing Composite " + (xattr xe "name") )
        | "enum" ->
          failwith "TODO enums"
          Console.WriteLine("Parsing Enum " + (xattr xe "name") )
        | "set" -> 
          failwith "TODO sets"
          Console.WriteLine("Parsing Set " + (xattr xe "name") )
        | _ -> failwith "unknown type"
      parseAux (parent.ElementsAfterSelf().First())
    and parseMessage (xeMessage) =
           
      Console.WriteLine("Parsing Message " + (xattr xeMessage "name") )
      
      let message = SbeMessage()
      message.Name <- xattr xeMessage "name"
      message.Id <- int <| xattrd xeMessage "id" "0"
      message.SemanticType <- xattr xeMessage "semanticType"
      message.Description <- xattr xeMessage "description"
      message.BlockLength <- int <| xattrd xeMessage "blockLength" "0"

      let rec processInnerElements (xes) offset : MessageElement seq =
        let list = List<MessageElement>()
        let mutable offset = offset
        for xe in xes  do
          match ln xe with
          | "field" ->
            let field = parseField xe offset
            offset <- offset + field.Size
            list.Add(field)
            Console.WriteLine("Parsing field " + (xattr xe "name") )
          | "group" ->
            let gr = new Group()
            gr.Name <- xattr xe "name"
            gr.Id <- int <| xattrd xe "id" "0"
            gr.DimensionType <- this.Types.[xattrd xe "dimensionType" "groupSizeEncoding"]
            gr.Description <- xattr xe "description"
            gr.BlockLength <- int <| xattrd xe "blockLength" "0"
            gr.GroupElements.AddRange((processInnerElements (xe.Elements()) 0))
            list.Add(gr)
            Console.WriteLine("Parsing group " + (xattr xe "name") )
          | "data" ->
            let data = new Data()
            data.Name <- xattr xe "name"
            data.Id <- int <| xattrd xe "id" "0"
            data.Type <- this.Types.[xattrd xe "type" "varDataEncoding"]
            data.Description <- xattr xe "description"
            data.SemanticType <- xattr xe "semanticType"
            data.SinceVersion <- int <| xattrd xe "sinceVersion" "0"
            list.Add(data)
            Console.WriteLine("Parsing data " + (xattr xe "name") )
          | _ -> failwith "unknown message element"
        list :> MessageElement seq

      and parseField fxe accOffset : Field =
        let field = new Field()
        field.Name <- xattr fxe "name"
        field.SinceVersion <- int <| xattrd fxe "sinceVersion" "0"
        let typeName = xattr fxe "type"
        let ty =
          if SbeMessageSchema.PrimitiveTypes.ContainsKey(typeName) then
            let pTy = SbeMessageSchema.PrimitiveTypes.[typeName]
            field.Offset <- accOffset
            field.Size <- pTy.Size
            pTy :> SbeType
          else
            let ty = this.Types.[typeName]
            match ty with 
            | :? SimpleType as sty -> 
              field.Offset <- accOffset
              field.Size <- sty.PrimitiveType.Size * sty.Length
            | :? CompositeType as cty ->
              field.Offset <- accOffset
              let mutable size = 0
              for sty in cty.InnerTypes do
                let compField = new Field()
                compField.Name <- sty.Name
                compField.Offset <- accOffset
                compField.Size <- sty.PrimitiveType.Size * sty.Length
                size <- size + compField.Size
                field.CompositeFields.Add(compField)
              field.Size <- size
            // TODO enums and sets
            | _ -> failwith "not supported type"
            ty
        field.Type <- ty
        field

      let elements = processInnerElements (xeMessage.Elements()) 0
      message.MessageElements.AddRange(elements)

      // next message
      try parseAux (xeMessage.ElementsAfterSelf().First())
      with
      | _ -> () // end
    parseAux document.Root