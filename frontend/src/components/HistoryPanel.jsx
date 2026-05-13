export default function HistoryPanel({ history, onView, onDelete }) {
  return (
    <div className="mt-10">
      <h2 className="text-xl mb-3 text-white">History</h2>

      {history.length === 0 && (
        <div className="text-gray-400">No history yet.</div>
      )}

      {history.map((item) => (
        <div
          key={item.id}
          className="bg-gray-800 p-3 mb-2 rounded flex justify-between items-center"
        >
          <div className="text-white">
            {item.language.toUpperCase()} | Score: {item.score}
          </div>

          <div className="flex gap-2">
            <button
              className="bg-blue-600 px-3 py-1 rounded hover:bg-blue-700"
              onClick={() => onView(item)}
            >
              View
            </button>

            <button
              className="bg-red-600 px-3 py-1 rounded hover:bg-red-700"
              onClick={() => onDelete(item.id)}
            >
              Delete
            </button>
          </div>
        </div>
      ))}
    </div>
  );
}