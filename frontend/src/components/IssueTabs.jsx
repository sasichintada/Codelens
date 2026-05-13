export default function IssueTabs({ errors, suggestions }) {
  return (
    <div className="mt-4">
      <h3 className="text-lg font-semibold mb-2">Issues</h3>
      {errors.length > 0 ? (
        errors.map((err, i) => (
          <div key={i} className="text-red-400">
            line {err.line}: {err.message}
          </div>
        ))
      ) : (
        <div className="text-green-400">No Syntax Errors</div>
      )}

      <h3 className="text-lg font-semibold mt-4">Suggestions</h3>
      {suggestions.map((s, i) => (
        <div key={i} className="text-yellow-400">{s}</div>
      ))}
    </div>
  );
}