
module MyTests {

  datatype VoidOutcome =
  | VoidSuccess
  | VoidFailure(error: string)
  {
      predicate method IsFailure() {
          this.VoidFailure?
      }
      function method PropagateFailure(): VoidOutcome requires IsFailure() {
          this
      }
  }

  function method {:test} PassingTest(): VoidOutcome {
    VoidSuccess
  }
}