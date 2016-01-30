#r "System.Xml.Linq"
#r "./bin/Release/Spreads.dll"
#r "./bin/Release/Spreads.Core.dll"
#r "./bin/Release/Spreads.Collections.dll"
#r "./bin/Release/Spreads.Extensions.dll"
//#load "ProvidedTypes.fs"
#load "SBEElements.fs"
//#r "./bin/Debug/SBETypeProvider.dll"
//open SBETypeProvider.Provided
#time "on"
//let mutable sum = 0L
//for i = 0 to 10000000 do
//  sum <- sum + int64 MyStruct.MyProperty2

//
//
//let tp = SBEProvider()

open SBETypeProvider
open System
open System.Linq
open System.Xml.Linq

car.Root.Name.LocalName

let parse (xd:XDocument) =
  let schema = MessageSchema()
  let rec parseAux (xe:XElement) =
    match ln xe with
    | "messageSchema" -> 
      schema.Package <- xe.Attribute(xn "package").Value
      parseAux (xe.Descendants().First())
    | "types" -> 
      parseTypes xe (xe.Descendants())
    | "message" -> 
      parseMessage xe (xe.Descendants())
    | _ as x ->
      Console.WriteLine("Unknowun element " + x)
  and parseTypes (parent) (xes:XElement seq) =
    for xe in xes do
      // TODO
      ()
    parseAux (parent.ElementsAfterSelf().First())
  and parseMessage (parent) (xes:XElement seq) =
    for xe in xes do
      // TODO
      ()
    parseAux (parent.ElementsAfterSelf().First())

  parseAux xd.Root

parse car