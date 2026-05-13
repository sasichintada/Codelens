import ReactDiffViewer from "react-diff-viewer-continued";

export default function DiffViewer({
  original,
  optimized,
  splitView = true
}) {
  return (
    <div className="mt-6 w-full overflow-auto max-h-[450px] border border-gray-700 rounded">
      <ReactDiffViewer
        oldValue={original || ""}
        newValue={optimized || ""}
        splitView={splitView}
        useDarkTheme={false}
        styles={{
          variables: {
            light: {
              diffViewerBackground: "#ffffff",
              diffViewerColor: "#000000",
              addedBackground: "#e6ffed",
              removedBackground: "#ffeef0",
              wordAddedBackground: "#acf2bd",
              wordRemovedBackground: "#fdb8c0",
            }
          },
          diffContainer: {
            overflowX: "auto",
            width: "100%",
            background: "#ffffff"
          },
          line: {
            padding: "4px 8px",
            fontSize: "14px",
            backgroundColor: "#ffffff"
          },
          gutter: {
            minWidth: "40px",
            backgroundColor: "#ffffff"
          },
          contentText: {
            wordBreak: "break-word",
            color: "#000000"
          }
        }}
      />
    </div>
  );
}