include "shared.dfy"

module TestDefiniteAssignment {
  import opened Shared

  datatype MyNewDataType = FirstExample | SecondExample

  // DefiniteAssignmentExampleFunction represents a sample function that fails verification when the definiteAssignment
  // parameter is passed into the VerifyDafnyPassthrough ItemGroup with a value or 2 or 3
  method DefiniteAssignmentExampleFunction(example: MyNewDataType) returns (tmp: nat)
  {
    match example {
      case FirstExample =>
        // We are not assigning anything here, which should trigger definite-assignment rules when definiteAssignment:2
        // or definiteAssignment:3 is used (since we will be returning tmp without defining it)
      case SecondExample =>
        tmp := 1;
    }
  }

  function method {:test} PassingTest(): VoidOutcome {
    VoidSuccess
  }
}
