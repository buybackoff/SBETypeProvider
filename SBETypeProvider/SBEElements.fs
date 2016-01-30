namespace SBETypeProvider

open System
open System.Linq
open System.Xml.Linq
open System.Collections.Generic

open Spreads

[<AutoOpenAttribute>]
module XmlHelper =
  let car = XDocument.Load(__SOURCE_DIRECTORY__ + "/Car.xml")
  let xn (s:string) = XName.op_Implicit(s)
  let ln (xe:XElement) = xe.Name.LocalName 


type SchemaElement() =
  /// Name of the message.
  member val Name = "" with get, set
  /// Unique ID of a message template.
  member val Id = 0 with get, set
  /// Description of the message. (optional)
  member val Description : string option = None with get, set
  ///  The semantic type of the message.
  member val SemanticType : string option = None with get, set


type SbeTypeName = string

type SbePresence =
| Required = 0
| Constant = 1
| Optional = 2

type PrimitiveType() =
  member val Type = Unchecked.defaultof<Type> with get, set
  member val Size = 0 with get, set

[<RequireQualifiedAccessAttribute>]
type SbeType = 
| Primitive of PrimitiveType
| Composite of SbeType seq
| GroupDimension of numInGroup:PrimitiveType * blockSize:PrimitiveType
| VarData of length:PrimitiveType * varData:PrimitiveType

type SbeElementName = string
type SbeOffset = int
type SbeVersion = int


type Field() =
  inherit SchemaElement()

type Group() =
  inherit SchemaElement()

type Data() =
  inherit SchemaElement()

type MessageElement =
  | Field of element:SchemaElement * ty:SbeType * offset:SbeOffset option * sinceVersion:SbeVersion option
  | Group of element:SchemaElement * dimensionType:SbeType * blockLength:int * nestedElements:MessageElement seq
  | Data of element:SchemaElement * varDataType:SbeType

type Message() =
  inherit SchemaElement()
  member val BlockLength : int option = None with get, set
  member val FixedFields = List<Field>() with get
  member val GroupFields = List<Group>() with get
  member val DataFields = List<Data>() with get


// TODO add offset properties where they are fixed, and a method to get offset

type MessageSchema(?filepath:string) as this =
  inherit SchemaElement()
  do
    () //this.ParseXml(filepath)
 
  member val Package = "" with get, set
  member val Version: SbeVersion = 0 with get, set
  member val ByteOrder: string = "littleEndian" with get, set
  member val Types = Dictionary<SbeTypeName, SbeType>() with get
  member val Messages = Dictionary<string, Message>() with get

  member private this.ParseXml(filepath:string) =
    // TODO to ctor
    failwith ""

  // we must learn how to get any property by name, then we could easily provide them
  member this.GetProperty(name:string, buffer: byte[]) =
    failwith "TODO"
