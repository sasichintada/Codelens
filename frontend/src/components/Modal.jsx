// src/components/Modal.jsx
import React from "react";
import DiffViewer from "./DiffViewer";

export default function Modal({ data, close }) {
  if (!data) return null;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-70 flex items-center justify-center z-50">
      <div className="bg-gray-900 p-6 rounded w-11/12 max-w-6xl relative">
        {/* Close button at top-right */}
        <button
          onClick={close}
          className="absolute top-3 right-4 text-white text-2xl font-bold hover:text-red-400"
        >
          ✖
        </button>

        {/* DiffViewer showing original vs optimized */}
        <DiffViewer original={data.original} optimized={data.optimized} />
      </div>
    </div>
  );
}