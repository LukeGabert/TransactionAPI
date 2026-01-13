import { useEffect, useState, useMemo } from 'react';
import { AlertTriangle, AlertCircle, Info, Loader2, Search, ShieldAlert, CheckCircle, Database, AlertTriangle as AlertIcon, CheckCircle2, X } from 'lucide-react';
import { riskReportService } from '../services/riskReportService';
import { transactionService } from '../services/transactionService';
import type { RiskReport, RiskLevel } from '../types/riskReport';
import { mapRiskLevel } from '../types/riskReport';
import Toast from './Toast';

const AnomalyDashboard = () => {
  const [riskReports, setRiskReports] = useState<RiskReport[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [toast, setToast] = useState<{ message: string; type: 'success' | 'error' | 'info' } | null>(null);
  const [resolvingIds, setResolvingIds] = useState<Set<number>>(new Set());
  const [scanning, setScanning] = useState<boolean>(false);
  const [searchQuery, setSearchQuery] = useState<string>('');
  const [totalResolved, setTotalResolved] = useState<number>(0);
  const [totalTransactionsScanned, setTotalTransactionsScanned] = useState<number>(0);
  const [selectedTransaction, setSelectedTransaction] = useState<RiskReport | null>(null);

  useEffect(() => {
    fetchRiskReports();
  }, []);

  const fetchRiskReports = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await riskReportService.getRiskReports();
      setRiskReports(data);
    } catch (err) {
      setError('Failed to fetch risk reports. Please try again later.');
      console.error('Error fetching risk reports:', err);
    } finally {
      setLoading(false);
    }
  };

  const getRiskBadge = (riskLevel: RiskLevel) => {
    switch (riskLevel) {
      case 'High':
        return (
          <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md text-xs font-medium bg-orange-100 text-orange-800 border border-orange-300">
            <ShieldAlert size={16} />
            High
          </span>
        );
      case 'Medium':
        return (
          <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md text-xs font-medium bg-yellow-100 text-yellow-800 border border-yellow-300">
            <AlertCircle size={16} />
            Medium
          </span>
        );
      case 'Low':
        return (
          <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md text-xs font-medium bg-blue-100 text-blue-800 border border-blue-300">
            <Info size={16} />
            Low
          </span>
        );
      default:
        return null;
    }
  };

  // Filter risk reports based on search query
  const filteredRiskReports = useMemo(() => {
    if (!searchQuery.trim()) {
      return riskReports;
    }

    const query = searchQuery.toLowerCase();
    return riskReports.filter((report) => {
      const transaction = report.transaction;
      return (
        report.transactionID.toLowerCase().includes(query) ||
        report.detectedAnomaly?.toLowerCase().includes(query) ||
        report.recommendedMitigation?.toLowerCase().includes(query) ||
        report.reasoning?.toLowerCase().includes(query) ||
        report.tldr?.toLowerCase().includes(query) ||
        transaction?.merchant?.toLowerCase().includes(query) ||
        transaction?.category?.toLowerCase().includes(query) ||
        transaction?.location?.toLowerCase().includes(query) ||
        transaction?.accountID?.toLowerCase().includes(query)
      );
    });
  }, [riskReports, searchQuery]);

  // Calculate stats
  const highRiskCount = riskReports.filter((report) => mapRiskLevel(report.riskLevel) === 'High').length;

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(amount);
  };

  const handleScanTransactions = async () => {
    setScanning(true);
    setError(null);

    try {
      const result = await transactionService.scanTransactions();
      // Update transactions analyzed count (cumulative total)
      setTotalTransactionsScanned(result.totalAnalyzed);
      setToast({
        message: result.transactionsAnalyzed > 0 
          ? `Scan completed! ${result.reportsCreated} risk report${result.reportsCreated !== 1 ? 's' : ''} created. Analyzed ${result.totalAnalyzed} of ${result.totalTransactions} transactions.`
          : result.message || 'All transactions have already been analyzed.',
        type: 'success',
      });
      // Refresh the risk reports list after scan
      await fetchRiskReports();
    } catch (err) {
      const errorMessage = (err as { response?: { data?: { detail?: string } }; message?: string })?.response?.data?.detail || (err as { message?: string })?.message || 'Unknown error occurred';
      
      // Provide user-friendly error messages based on error type
      let userMessage = 'Failed to scan transactions. ';
      if (errorMessage.includes('rate limit') || errorMessage.includes('429')) {
        userMessage += 'OpenAI API rate limit exceeded. Please wait a few minutes and try again.';
      } else if (errorMessage.includes('API key') || errorMessage.includes('401') || errorMessage.includes('Unauthorized')) {
        userMessage += 'Invalid API key. Please check your OpenAI API key configuration.';
      } else {
        userMessage += errorMessage;
      }
      
      setToast({
        message: userMessage,
        type: 'error',
      });
      console.error('Error scanning transactions:', err);
    } finally {
      setScanning(false);
    }
  };

  const handleResolve = async (reportID: number) => {
    // Optimistically update the UI - remove the item immediately
    setRiskReports((prev) => prev.filter((report) => report.reportID !== reportID));
    setResolvingIds((prev) => new Set(prev).add(reportID));

    try {
      await riskReportService.resolveRiskReport(reportID);
      // Increment resolved count
      setTotalResolved((prev) => prev + 1);
      // Show success toast
      setToast({ message: 'Risk report marked as resolved', type: 'success' });
    } catch (err) {
      // Revert the optimistic update on error
      fetchRiskReports();
      setToast({
        message: 'Failed to resolve risk report. Please try again.',
        type: 'error',
      });
      console.error('Error resolving risk report:', err);
    } finally {
      setResolvingIds((prev) => {
        const newSet = new Set(prev);
        newSet.delete(reportID);
        return newSet;
      });
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="text-center">
          <Loader2 className="w-8 h-8 animate-spin text-blue-500 mx-auto mb-4" />
          <p className="text-gray-600 dark:text-gray-400">Loading risk reports...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="text-center">
          <AlertTriangle className="w-8 h-8 text-red-500 mx-auto mb-4" />
          <p className="text-red-600 dark:text-red-400">{error}</p>
          <button
            onClick={fetchRiskReports}
            className="mt-4 px-4 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 transition-colors"
          >
            Retry
          </button>
        </div>
      </div>
    );
  }

  return (
    <>
      {toast && (
        <Toast
          message={toast.message}
          type={toast.type}
          onClose={() => setToast(null)}
        />
      )}
      <div className="bg-white dark:bg-gray-900">
        <div className="mb-8">
          <h1 className="text-2xl font-semibold text-gray-900 dark:text-white mb-1">
            Anomaly Detection Dashboard
          </h1>
          <p className="text-sm text-gray-500 dark:text-gray-400">
            {filteredRiskReports.length} flagged transaction{filteredRiskReports.length !== 1 ? 's' : ''} detected
          </p>
        </div>

        {/* Stats Cards */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-8">
          {/* Transactions Analyzed */}
          <div className="bg-white dark:bg-gray-800 rounded-md border border-gray-200 dark:border-gray-700 p-6 flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-600 dark:text-gray-400 mb-1">Transactions Analyzed</p>
              <p className="text-3xl font-semibold text-gray-900 dark:text-white">{totalTransactionsScanned}</p>
            </div>
            <Database size={20} className="text-gray-400" />
          </div>

          {/* High-Risk Alerts */}
          <div className="bg-white dark:bg-gray-800 rounded-md border border-orange-300 p-6 flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-orange-700 mb-1">High-Risk Alerts</p>
              <p className="text-3xl font-semibold text-orange-700">{highRiskCount}</p>
            </div>
            <AlertIcon size={20} className="text-orange-600" />
          </div>

          {/* Total Resolved */}
          <div className="bg-white dark:bg-gray-800 rounded-md border border-gray-200 dark:border-gray-700 p-6 flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-600 dark:text-gray-400 mb-1">Total Resolved</p>
              <p className="text-3xl font-semibold text-gray-900 dark:text-white">{totalResolved}</p>
            </div>
            <CheckCircle2 size={20} className="text-gray-400" />
          </div>
        </div>

        <div className="mb-6 flex items-center justify-between">
          <div className="flex-1 max-w-md">
            {riskReports.length > 0 && (
              <div className="relative">
                <Search size={18} className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400" />
                <input
                  type="text"
                  placeholder="Search transactions, merchants, anomalies..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="w-full pl-10 pr-4 py-2.5 border border-gray-200 dark:border-gray-700 rounded-md bg-white dark:bg-gray-800 text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent text-sm"
                />
              </div>
            )}
          </div>
          <button
            onClick={handleScanTransactions}
            disabled={scanning || loading}
            className="inline-flex items-center gap-2 px-4 py-2.5 text-sm font-medium text-white bg-gray-900 dark:bg-gray-700 hover:bg-gray-800 dark:hover:bg-gray-600 disabled:bg-gray-300 disabled:cursor-not-allowed rounded-md transition-colors"
          >
            {scanning ? (
              <>
                <Loader2 size={18} className="animate-spin" />
                Scanning Transactions...
              </>
            ) : (
              <>
                <Search size={18} />
                Scan Transactions
              </>
            )}
          </button>
        </div>

      {filteredRiskReports.length === 0 ? (
        <div className="text-center py-16 bg-white dark:bg-gray-800 rounded-md border border-gray-200 dark:border-gray-700">
          <Info size={48} className="text-gray-400 mx-auto mb-4" />
          <p className="text-gray-600 dark:text-gray-400">
            {riskReports.length === 0 
              ? 'No risk reports found.' 
              : 'No risk reports match your search.'}
          </p>
        </div>
      ) : (
        <div className="bg-white dark:bg-gray-800 rounded-md border border-gray-200 dark:border-gray-700 overflow-hidden">
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
              <thead className="bg-gray-50 dark:bg-gray-900/50">
                <tr>
                  <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Transaction ID
                  </th>
                  <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Risk Level
                  </th>
                  <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Merchant
                  </th>
                  <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Amount
                  </th>
                  <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Risk Summary
                  </th>
                  <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    AI-Recommended Mitigation
                  </th>
                  <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Actions
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700">
              {filteredRiskReports.map((report) => (
                <tr
                  key={report.reportID}
                  onClick={() => setSelectedTransaction(report)}
                  className="hover:bg-gray-50/50 dark:hover:bg-gray-700/30 transition-colors cursor-pointer"
                >
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm font-medium text-gray-900 dark:text-white">
                      {report.transactionID}
                    </div>
                    {report.transaction && (
                      <div className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
                        {formatDate(report.transaction.timestamp)}
                      </div>
                    )}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    {getRiskBadge(mapRiskLevel(report.riskLevel))}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    {report.transaction ? (
                      <div>
                        <div className="text-sm text-gray-900 dark:text-white">
                          {report.transaction.merchant}
                        </div>
                        <div className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
                          {report.transaction.category}
                        </div>
                        <div className="text-xs text-gray-400 dark:text-gray-500 mt-0.5">
                          {report.transaction.location}
                        </div>
                      </div>
                    ) : (
                      <span className="text-sm text-gray-500 dark:text-gray-400">N/A</span>
                    )}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    {report.transaction ? (
                      <div className="text-sm font-medium text-gray-900 dark:text-white">
                        {formatCurrency(report.transaction.amount)}
                      </div>
                    ) : (
                      <span className="text-sm text-gray-500 dark:text-gray-400">N/A</span>
                    )}
                  </td>
                  <td className="px-6 py-4">
                    <div className="text-xs font-bold text-gray-900 dark:text-white max-w-xs">
                      {report.tldr || (
                        <span className="text-gray-400 dark:text-gray-500 font-normal">No summary</span>
                      )}
                    </div>
                  </td>
                  <td className="px-6 py-4">
                    <div className="text-sm text-gray-900 dark:text-white max-w-md">
                      {report.recommendedMitigation || (
                        <span className="text-gray-400 dark:text-gray-500">No recommendation</span>
                      )}
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <button
                      onClick={() => handleResolve(report.reportID)}
                      disabled={resolvingIds.has(report.reportID)}
                      className="inline-flex items-center gap-2 px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-300 rounded-md hover:border-blue-500 hover:text-blue-600 dark:hover:border-blue-500 dark:hover:text-blue-400 disabled:bg-gray-100 disabled:text-gray-400 disabled:border-gray-200 disabled:cursor-not-allowed transition-colors"
                    >
                      {resolvingIds.has(report.reportID) ? (
                        <>
                          <Loader2 size={18} className="animate-spin" />
                          Resolving...
                        </>
                      ) : (
                        <>
                          <CheckCircle size={18} />
                          Resolve
                        </>
                      )}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        </div>
      )}
      </div>

      {/* Sliding Sidebar */}
      <div
        className={`fixed top-0 right-0 h-full w-full max-w-lg bg-white dark:bg-gray-800 shadow-2xl z-50 transform transition-transform duration-300 ease-in-out ${
          selectedTransaction ? 'translate-x-0' : 'translate-x-full'
        }`}
      >
        {selectedTransaction && (
          <div className="flex flex-col h-full">
            {/* Header */}
            <div className="flex items-center justify-between p-6 border-b border-gray-200 dark:border-gray-700">
              <h2 className="text-xl font-semibold text-gray-900 dark:text-white">
                Transaction Details
              </h2>
              <button
                onClick={(e) => {
                  e.stopPropagation();
                  setSelectedTransaction(null);
                }}
                className="p-2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 rounded-md hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
              >
                <X size={20} />
              </button>
            </div>

            {/* Content */}
            <div className="flex-1 overflow-y-auto p-6">
              {/* Transaction Info */}
              <div className="mb-6">
                <h3 className="text-sm font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3">
                  Transaction Information
                </h3>
                <div className="space-y-3">
                  <div>
                    <span className="text-sm text-gray-500 dark:text-gray-400">Transaction ID:</span>
                    <p className="text-sm font-medium text-gray-900 dark:text-white mt-1">
                      {selectedTransaction.transactionID}
                    </p>
                  </div>
                  {selectedTransaction.transaction && (
                    <>
                      <div>
                        <span className="text-sm text-gray-500 dark:text-gray-400">Merchant:</span>
                        <p className="text-sm font-medium text-gray-900 dark:text-white mt-1">
                          {selectedTransaction.transaction.merchant}
                        </p>
                      </div>
                      <div>
                        <span className="text-sm text-gray-500 dark:text-gray-400">Amount:</span>
                        <p className="text-sm font-medium text-gray-900 dark:text-white mt-1">
                          {formatCurrency(selectedTransaction.transaction.amount)}
                        </p>
                      </div>
                      <div>
                        <span className="text-sm text-gray-500 dark:text-gray-400">Category:</span>
                        <p className="text-sm font-medium text-gray-900 dark:text-white mt-1">
                          {selectedTransaction.transaction.category}
                        </p>
                      </div>
                      <div>
                        <span className="text-sm text-gray-500 dark:text-gray-400">Date:</span>
                        <p className="text-sm font-medium text-gray-900 dark:text-white mt-1">
                          {formatDate(selectedTransaction.transaction.timestamp)}
                        </p>
                      </div>
                      <div>
                        <span className="text-sm text-gray-500 dark:text-gray-400">Location:</span>
                        <p className="text-sm font-medium text-gray-900 dark:text-white mt-1">
                          {selectedTransaction.transaction.location}
                        </p>
                      </div>
                    </>
                  )}
                  <div>
                    <span className="text-sm text-gray-500 dark:text-gray-400">Risk Level:</span>
                    <div className="mt-1">
                      {getRiskBadge(mapRiskLevel(selectedTransaction.riskLevel))}
                    </div>
                  </div>
                </div>
              </div>

              {/* AI Audit Section */}
              <div className="mb-6">
                <h3 className="text-sm font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3">
                  AI Audit
                </h3>
                <div className="bg-gray-50 dark:bg-gray-900/50 rounded-md p-4">
                  {selectedTransaction.reasoning ? (
                    <p className="text-sm text-gray-900 dark:text-white whitespace-pre-wrap">
                      {selectedTransaction.reasoning}
                    </p>
                  ) : (
                    <p className="text-sm text-gray-400 dark:text-gray-500">
                      No reasoning provided
                    </p>
                  )}
                </div>
              </div>

              {/* Additional Details */}
              {selectedTransaction.recommendedMitigation && (
                <div className="mb-6">
                  <h3 className="text-sm font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3">
                    Recommended Mitigation
                  </h3>
                  <p className="text-sm text-gray-900 dark:text-white">
                    {selectedTransaction.recommendedMitigation}
                  </p>
                </div>
              )}
            </div>

            {/* Footer with Buttons */}
            <div className="border-t border-gray-200 dark:border-gray-700 p-6 flex gap-3">
              <button
                onClick={(e) => {
                  e.stopPropagation();
                  setSelectedTransaction(null);
                }}
                className="flex-1 px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-300 rounded-md hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors"
              >
                Close
              </button>
              <button
                onClick={(e) => {
                  e.stopPropagation();
                  if (selectedTransaction) {
                    handleResolve(selectedTransaction.reportID);
                    setSelectedTransaction(null);
                  }
                }}
                className="flex-1 px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-md hover:bg-blue-700 transition-colors"
              >
                Acknowledge
              </button>
            </div>
          </div>
        )}
      </div>

      {/* Backdrop */}
      {selectedTransaction && (
        <div
          className="fixed inset-0 bg-black bg-opacity-50 z-40"
          onClick={() => setSelectedTransaction(null)}
        />
      )}
    </>
  );
};

export default AnomalyDashboard;
