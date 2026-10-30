export type ProblemDetails = Readonly<{
  title?: string;
  detail?: string;
  status?: number;
  errors?: Readonly<Record<string, readonly string[]>>;
  correlationId?: string;
}>;

export class AdminApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly problem: ProblemDetails,
  ) {
    super(problem.detail || problem.title || `Yêu cầu thất bại (HTTP ${status}).`);
    this.name = "AdminApiError";
  }
}

export async function readProblem(response: Response): Promise<ProblemDetails> {
  try {
    const value: unknown = await response.json();
    if (typeof value === "object" && value !== null) {
      return value as ProblemDetails;
    }
  } catch {
    // The fallback below avoids exposing an upstream response body.
  }

  return { status: response.status, title: "Yêu cầu không thành công." };
}
