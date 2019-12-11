
include "shared.dfy"

module MyTests {
  import opened Shared

  function method {:test} PassingTest(): VoidOutcome {
    VoidSuccess
  }
}
