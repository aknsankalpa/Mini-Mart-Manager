# RetailFlow (MiniMart Manager) — Testing Checklist

Manual test pass executed against a freshly seeded database (see `Data/DbInitializer.cs`),
driven end-to-end through the running application (not just code review). Each item below
was actually exercised and its real, observed result recorded — not assumed.

Legend: ✅ = passed as expected.

## Product Tests

| # | Test | Steps | Expected | Result |
|---|------|-------|----------|--------|
| 1 | Add valid product | Fill in code `TST001`, name, price 199, stock 50, reorder 10 → Save | "Product added successfully." | ✅ |
| 2 | Reject empty name | Leave Name blank, fill the rest → Save | "Product name is required." | ✅ |
| 3 | Reject negative price | Price = `-10` | "Price must be greater than 0." | ✅ |
| 4 | Reject negative stock | Stock = `-5` | "Stock quantity cannot be negative." | ✅ |
| 5 | Reject duplicate product code | Code = `TST001` (already exists) | "Product code already exists." | ✅ |
| 6 | Edit product | Select `TST001`, change price to 249 → Save | "Product updated successfully."; grid shows Rs. 249.00 | ✅ |
| 7 | Search product | Type `rice` in the search box | Grid filters to exactly 1 matching row | ✅ |
| 8 | Deactivate product | Select `TST001` → Deactivate → confirm | "Product deactivated."; hidden from the active list, visible with "Show inactive" | ✅ |

## Sales Tests

| # | Test | Steps | Expected | Result |
|---|------|-------|----------|--------|
| 1 | Add product to cart | Select Rice, qty 2 → Add to Cart | 1 cart row, Rice × 2 | ✅ |
| 2 | Change quantity | Add Rice again, qty 1 | Existing line merges to qty 3 (not a duplicate row) | ✅ |
| 3 | Remove product | Add Sugar, then click ✕ on its row | Row removed; Subtotal recalculates (4,100.00 → 3,750.00) | ✅ |
| 4 | Calculate subtotal | Cart: Rice×3 (Rs. 1,250) + Sugar×1 (Rs. 350) | Subtotal = Rs. 4,100.00 | ✅ |
| 5 | Apply discount | Set discount to 150 | Accepted, reflected in Total | ✅ |
| 6 | Calculate total | Subtotal 3,750.00 − Discount 150.00 | Total = Rs. 3,600.00 | ✅ |
| 7 | Complete sale | Click Complete Sale | Confirmation dialog with invoice number; cart clears | ✅ (`INV-20260909-004`) |
| 8 | Reject empty sale | Cart empty (post-completion) | Complete Sale button disabled (`IsEnabled = False`) | ✅ |
| 9 | Reject zero quantity | Quantity = `0` → Add to Cart | "Quantity must be a whole number greater than 0." | ✅ |
| 10 | Reject quantity greater than stock | Quantity = `99999` on a product with limited stock | "Insufficient Stock — Only N units of X are currently available." | ✅ |

## Stock Tests

| # | Test | Steps | Expected | Result |
|---|------|-------|----------|--------|
| 1 | Stock decreases after sale | Rice stock before/after selling 3 units | 40 → 37 | ✅ |
| 2 | Stock never becomes negative | Attempt to sell more than available (Sales Test #10) | Blocked before the sale can complete; stock unaffected | ✅ |
| 3 | Low-stock alert appears correctly | Dashboard "Low Stock" tile vs. Products' "Low stock only" filter | Both report 6 products, and list the same 6 (Sugar, Black Tea, Milk Powder, Chocolate Bar, Detergent Powder, Hand Sanitizer) | ✅ |

## Transaction Tests

| # | Test | Steps | Expected | Result |
|---|------|-------|----------|--------|
| 1 | Completed sale appears in history | Open Transaction History after completing a sale | New invoice listed | ✅ (`INV-20260909-004`) |
| 2 | Invoice details are correct | Select the invoice | Line items, Subtotal (3,750.00), Discount (150.00), Total (3,600.00) all match what was sold | ✅ |
| 3 | Sale remains after restarting application | Close and reopen the app, reopen Transaction History | Invoice still present with the same figures | ✅ |

## Persistence Tests

| # | Test | Steps | Expected | Result |
|---|------|-------|----------|--------|
| 1 | Close application | — | Closes normally | ✅ |
| 2 | Reopen application | Launch again | Starts normally against the existing database (no re-seed, no crash) | ✅ |
| 3 | Verify products remain | Check `TST001` | Present, still shows the edited price (249.00) and Inactive status | ✅ |
| 4 | Verify transactions remain | Check `INV-20260909-004` | Present with identical Subtotal/Discount/Total | ✅ |
| 5 | Verify stock remains correct | Check Rice and Sugar stock | Rice = 37, Sugar = 3 (unchanged from before the restart) | ✅ |

## Notes

- All 29 checks passed on this run. No failures were found; none of the fixes made in
  earlier phases (case-insensitive search, resilient ViewModel loading, centralized
  subtotal/total calculation) needed further changes as a result of this pass.
- Tests were executed via Windows UI Automation driving the actual compiled application
  (not a separate unit test project) — this app has no automated test suite; this
  checklist is the verification record for a manual/scripted QA pass.
- Test artifacts (the `TST001` product and its sale) were created against a disposable
  copy of the seeded database for this run and are not part of the shipped seed data in
  `Data/DbInitializer.cs`.
