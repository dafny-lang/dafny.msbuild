  
  module Shared {
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
  }