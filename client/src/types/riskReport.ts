export type RiskLevel = 'Low' | 'Medium' | 'High';

// Helper function to convert numeric enum from .NET API to RiskLevel string
export const mapRiskLevel = (value: number | string): RiskLevel => {
  if (typeof value === 'string') {
    return value as RiskLevel;
  }
  switch (value) {
    case 0:
      return 'Low';
    case 1:
      return 'Medium';
    case 2:
      return 'High';
    default:
      return 'Low';
  }
};

export interface RiskReport {
  reportID: number;
  transactionID: string;
  riskLevel: RiskLevel | number; // Accept both string and number from API
  detectedAnomaly: string | null;
  recommendedMitigation: string | null;
  transaction?: {
    transactionID: string;
    accountID: string;
    amount: number;
    merchant: string;
    category: string;
    timestamp: string;
    location: string;
  };
}
