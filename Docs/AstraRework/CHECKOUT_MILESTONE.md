# Checkout, opening hours and input recovery milestone

Completed the interrupted regression change on 6 September 2026. Closing the shop now stops arrivals while current customers finish. Cash sales save the sold identity, career balance and exact drawer denominations together; an abandoned unpaid ticket clears, an empty till stays empty after load, and a drawer conflict refuses settlement until a valid retry.

The counter had disabled the same Player action map it reads. It now keeps remapped station actions available with locomotion locked, while nested ordinary menus still block those actions. Actual keyboard retry banked one sale; gamepad South scanned and East exited without moving the player or banking an unpaid ticket. Exact-cents checkout formatting also fixes the POS showing $46 for a $45.95 card payment.

Validation: 50 focused EditMode cases passed, compilation clean, no runtime errors in the final isolated session. Actual 4/5/9/5/Enter showed and authorized $45.95; the same specimen left, cash and disk balance became $245.95, and drawer denominations stayed unchanged. Workshop was byte-identical and clean; all protected player files matched the final session's preparation hashes after exit.

- [Atomic save and conflict proof](CHECKOUT_ATOMIC_SAVE_EVIDENCE.json)
- [Abandonment and subsequent sale](CHECKOUT_ABANDONMENT_DIAGNOSTIC.json)
- [Keyboard and controller input proof](CHECKOUT_INPUT_RECOVERY_EVIDENCE.json)
- [Exact cents and actual card-key proof](CHECKOUT_CENTS_EVIDENCE.json)
- [POS and terminal capture](Checkout/pos-and-terminal.png), [typed amount](Checkout/typed-45-95.png)
- [Isolation sessions](SAVE_ISOLATION_EVIDENCE.json) and [interruption boundary evidence](PLAYER_DATA_RECOVERY_BOUNDARY.json)

These are labelled diagnostic fixtures with injected stock/shop setup, not the mandatory full fresh career. Full controller sales, checkout art/HUD, the measured geometry hitch, whole-game QA and Steam readiness remain open. The next work returns immediately to the master-spec truth audit, benchmarking, concept selection and whole-shop architecture phases.
