const defaultApiBaseUrl = "http://localhost:5080/api/v1";

function getApiBaseUrl(): string {
  const configuredUrl = process.env.API_BASE_URL?.trim() || defaultApiBaseUrl;

  try {
    return new URL(configuredUrl).toString().replace(/\/$/, "");
  } catch {
    throw new Error("API_BASE_URL phải là URL tuyệt đối hợp lệ.");
  }
}

export const apiConfig = Object.freeze({
  baseUrl: getApiBaseUrl(),
});

