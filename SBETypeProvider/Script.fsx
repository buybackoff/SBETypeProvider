//#load "ProvidedTypes.fs"
//#load "SBETypeProvider.fs"
//#r "./bin/Debug/SBETypeProvider.dll"
open SBETypeProvider.Provided
#time "on"
let mutable sum = 0L
for i = 0 to 10000000 do
  sum <- sum + int64 MyStruct.MyProperty2

//
//
//let tp = SBEProvider()