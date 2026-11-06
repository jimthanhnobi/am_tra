import test from "node:test";
import assert from "node:assert/strict";
import { vietnamMonthRange, vietnamDateTimeToUtc, validReportDates } from "../src/lib/business-calendar.ts";
import { parseCustomers } from "../src/lib/customer-import.ts";
import { trustedClientIp } from "../src/lib/api/trusted-client-ip.ts";

test("client IP forwarding is opt-in, ignores arbitrary chains and validates a single address", () => {
  const headers = new Headers({ "x-real-ip": "192.0.2.42", "x-forwarded-for": "198.51.100.1, 203.0.113.1" });
  assert.equal(trustedClientIp(headers, false), null);
  assert.equal(trustedClientIp(headers, true), "192.0.2.42");
  for (const value of ["192.0.2.1, 192.0.2.2", "invalid", "999.1.1.1", ""]) {
    assert.equal(trustedClientIp(new Headers({ "x-real-ip": value }), true), null);
  }
  assert.equal(trustedClientIp(new Headers({ "x-real-ip": "2001:db8::1" }), true), "2001:db8::1");
});

test("Vietnam month boundary and leap year ranges", () => {
  assert.deepEqual(vietnamMonthRange(new Date("2026-11-30T17:00:00Z")), { fromDate: "2026-12-01", toDate: "2027-01-01" });
  assert.deepEqual(vietnamMonthRange(new Date("2026-11-30T16:59:59Z")), { fromDate: "2026-11-01", toDate: "2026-12-01" });
  assert.equal(vietnamDateTimeToUtc("2026-12-01T06:00"), "2026-11-30T23:00:00.000Z");
  assert.ok(validReportDates("2024-02-29", "2024-03-01"));
  for (const [from, to] of [["2026-02-29", "2026-03-01"], ["bad", "2026-03-01"], ["2026-03-01", "2026-03-01"], ["2024-01-01", "2026-01-01"]]) assert.equal(validReportDates(from, to), false);
});

test("Vietnamese CSV headers, per-row tags/history/source/notes and quoted commas", () => {
  const rows = parseCustomers('\uFEFFTên,Số điện thoại,Đã mua,Tags,Ghi chú,Nguồn\r\n"Khách, A",0912345678,Có,wholesale;vip,"Dòng 1\nDòng 2",referral\r\nKhách B,0987654321,Không,,,\r\n', "unknown", "end_user");
  assert.equal(rows.length, 2);
  assert.equal(rows[0].name, "Khách, A");
  assert.equal(rows[0].hadPriorPurchase, true);
  assert.deepEqual(rows[0].tags, ["wholesale", "vip"]);
  assert.equal(rows[0].notes, "Dòng 1\nDòng 2");
  assert.equal(rows[0].acquisitionSource, "referral");
  assert.equal(rows[1].hadPriorPurchase, false);
  assert.equal(rows[1].acquisitionSource, "unknown");
  assert.deepEqual(rows[1].tags, ["end_user"]);
});

test("bad CSV rows do not prevent valid rows being previewed", () => {
  const rows = parseCustomers("name,phone,da_mua\nKhách A,0912345678,maybe\nKhách B\nKhách C,0987654321,true", "unknown", "");
  assert.ok(rows[0].importError);
  assert.ok(rows[1].importError);
  assert.equal(rows[2].importError, null);
  assert.equal(rows[2].hadPriorPurchase, true);
  assert.throws(() => parseCustomers('name,phone\n"broken', "unknown", ""));
});
