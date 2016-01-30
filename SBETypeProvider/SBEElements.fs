module SBEElements

open System
open System.Collections.Generic



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


type MessageSchema() =
  inherit SchemaElement()
  member val Package = "" with get, set
  member val Version: SbeVersion = 0 with get, set
  member val ByteOrder: string = "littleEndian" with get, set
  member val Types = Dictionary<SbeTypeName, SbeType>() with get
  member val Message = Message() with get