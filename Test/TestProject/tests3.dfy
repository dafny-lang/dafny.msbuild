include "shared.dfy"

module MyTests3 {
  import opened Shared

  function method {:test} PassingTest(): VoidOutcome {
    VoidSuccess
  }
}
