export function vietnamMonthRange(now = new Date()) {
  const local = new Date(now.getTime() + 7 * 60 * 60 * 1000);
  const year = local.getUTCFullYear();
  const month = local.getUTCMonth();
  return {
    fromDate: new Date(Date.UTC(year, month, 1)).toISOString().slice(0, 10),
    toDate: new Date(Date.UTC(year, month + 1, 1)).toISOString().slice(0, 10),
  };
}

export function vietnamLocalDateTime(now = new Date()) {
  return new Date(now.getTime() + 7 * 60 * 60 * 1000).toISOString().slice(0, 16);
}

export function vietnamDateTimeToUtc(value: string) {
  return new Date(`${value}:00+07:00`).toISOString();
}

export function validReportDates(from: string, to: string) {
  const valid = (value: string) => /^\d{4}-\d{2}-\d{2}$/.test(value) &&
    Number.isFinite(Date.parse(value)) && new Date(value).toISOString().slice(0, 10) === value;
  const days = (Date.parse(to) - Date.parse(from)) / 86400000;
  return valid(from) && valid(to) && days > 0 && days <= 366;
}
