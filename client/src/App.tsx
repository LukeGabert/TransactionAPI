import AnomalyDashboard from './components/AnomalyDashboard';
import './App.css';

function App() {
  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-950 p-8">
      <div className="max-w-7xl mx-auto">
        <AnomalyDashboard />
      </div>
    </div>
  );
}

export default App;
