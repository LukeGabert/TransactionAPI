import api from './api';

export const transactionService = {
  /**
   * Triggers a scan of transactions for anomaly detection using AI
   */
  async scanTransactions(): Promise<{ message: string; reportsCreated: number; transactionsAnalyzed: number; totalAnalyzed: number; totalTransactions: number }> {
    const response = await api.post<{ message: string; reportsCreated: number; transactionsAnalyzed: number; totalAnalyzed: number; totalTransactions: number }>('/transactions/scan');
    return response.data;
  },
  /**
   * Gets transaction statistics
   */
  async getTransactionStats(): Promise<{ totalTransactions: number }> {
    const response = await api.get<{ totalTransactions: number }>('/transactions/stats');
    return response.data;
  },
};
