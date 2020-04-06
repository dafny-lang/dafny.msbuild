include "shared.dfy"

module MyTests2 {
  import opened Shared
  
  function method {:test} PassingTest(): VoidOutcome {
    VoidSuccess
  }
}
