export default function CodeEditor({ code, setCode, language }) {
  return (
    <textarea
      value={code}
      onChange={(e) => setCode(e.target.value)}
      className="w-full h-96 bg-gray-900 p-4 font-mono text-xl rounded text-white"
      placeholder={`Write ${language} code here...`}
    />
  );
}