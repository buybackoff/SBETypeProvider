namespace SBETypeProviderTest
open SBETypeProvider
open System
open System.Linq

type Sbe = SbeProvider<"..\..\..\SBETypeProvider\Car.xml">

module Test =
  let sbe = Sbe()
  let schema = sbe.Schema

  [<EntryPoint>]
  let print _ =
    //let ft = SBETypeProvider.MyStruct.MyProperty2
    //let sc = Sbe().Schema
  //let xdoc = XmlHelper.car
//    let schema = sbe.Schema
//    let schema = sbe.Schema
//    schema.Messages.Count
//    let value = new SbeProvider<"">() // .MyStruct.MyProperty2
//    
//    System.Console.WriteLine(value)
    Console.ReadLine() |> ignore
    0
