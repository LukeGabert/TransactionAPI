import api from './api';
import type { RiskReport } from '../types/riskReport';

export const riskReportService = {
  /**
   * Fetches all risk reports from the API
   */
  async getRiskReports(): Promise<RiskReport[]> {
    const response = await api.get<RiskReport[]>('/risk-reports');
    return response.data;
  },

  /**
   * Fetches a single risk report by ID
   */
  async getRiskReportById(reportID: number): Promise<RiskReport> {
    const response = await api.get<RiskReport>(`/risk-reports/${reportID}`);
    return response.data;
  },

  /**
   * Marks a risk report as resolved/mitigated
   */
  async resolveRiskReport(reportID: number): Promise<void> {
    await api.put(`/risk-reports/${reportID}/resolve`);
  },
};
