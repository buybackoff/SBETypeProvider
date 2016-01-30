namespace SBETypeProvider
open System

// proof of concept
module SBETest =
  let schema = "<type name=\"TheNumber\" primitiveType=\"uint16\" semanticType=\"TheNumber\"/>"
  let payload = BitConverter.GetBytes(42us)

  // need to get something like Assert.AreEqual(42us, SBE(schema, payload).TheNumber)


