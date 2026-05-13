import { useState } from "react";

const API_BASE_URL = "http://localhost:5120/api";

export default function Auth({ onLogin }) {
  const [isLogin, setIsLogin] = useState(true);
  const [formData, setFormData] = useState({
    firstName: "",
    lastName: "",
    email: "",
    password: "",
    confirmPassword: ""
  });
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const [successMessage, setSuccessMessage] = useState("");

  const handleChange = (e) => {
    setFormData({
      ...formData,
      [e.target.name]: e.target.value
    });
    // Clear error when user types
    setError("");
  };

  const validatePassword = (password) => {
    const errors = [];
    if (password.length < 6) errors.push("Password must be at least 6 characters");
    if (!/[A-Z]/.test(password)) errors.push("Password must contain at least one uppercase letter");
    if (!/[a-z]/.test(password)) errors.push("Password must contain at least one lowercase letter");
    if (!/[0-9]/.test(password)) errors.push("Password must contain at least one number");
    return errors;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");
    setSuccessMessage("");
    setLoading(true);

    // Registration validation
    if (!isLogin) {
      if (formData.password !== formData.confirmPassword) {
        setError("Passwords don't match!");
        setLoading(false);
        return;
      }

      const passwordErrors = validatePassword(formData.password);
      if (passwordErrors.length > 0) {
        setError(passwordErrors.join("\n"));
        setLoading(false);
        return;
      }
    }

    const endpoint = isLogin ? "login" : "register";
    
    // Create payload based on login/register
    const payload = isLogin 
      ? { email: formData.email, password: formData.password }
      : {
          firstName: formData.firstName.trim(),
          lastName: formData.lastName.trim(),
          email: formData.email,
          password: formData.password,
          confirmPassword: formData.confirmPassword
        };

    try {
      console.log(`Sending request to ${API_BASE_URL}/Auth/${endpoint}`, payload);

      const response = await fetch(`${API_BASE_URL}/Auth/${endpoint}`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(payload),
      });

      const data = await response.json();
      console.log("Response received:", data);

      if (!response.ok) {
        // Handle different error formats
        if (data.errors) {
          // Handle array of errors
          if (Array.isArray(data.errors)) {
            throw new Error(data.errors.join("\n"));
          } else {
            // Handle object with error fields
            const errorMessages = Object.values(data.errors).flat();
            throw new Error(errorMessages.join("\n"));
          }
        } else if (data.message) {
          throw new Error(data.message);
        } else if (data.error) {
          throw new Error(data.error);
        } else {
          throw new Error("Authentication failed. Please try again.");
        }
      }

      if (isLogin) {
        // Login successful - save session data
        localStorage.setItem("token", data.token);
        localStorage.setItem("sessionId", data.sessionId);
        localStorage.setItem("user", JSON.stringify({
          email: data.email,
          firstName: data.firstName,
          lastName: data.lastName
        }));
        
        // Set session ID in headers for future requests
        onLogin(data);
      } else {
        // Registration successful
        setSuccessMessage("Registration successful! Please login.");
        setIsLogin(true);
        setFormData({
          firstName: "",
          lastName: "",
          email: "",
          password: "",
          confirmPassword: ""
        });
      }
    } catch (err) {
      console.error("Auth error:", err);
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-[#0b1220] flex items-center justify-center p-4">
      <div className="bg-[#111827] border border-gray-800 rounded-xl p-8 w-full max-w-md">
        {/* Logo/Title */}
        <div className="text-center mb-8">
          <h1 className="text-3xl font-semibold text-emerald-400 mb-2">
            🚀 CodeLens
          </h1>
          <p className="text-gray-400">
            {isLogin ? "Welcome back!" : "Create your account"}
          </p>
        </div>

        {/* Success Message */}
        {successMessage && (
          <div className="mb-6 bg-emerald-900/30 border border-emerald-800 text-emerald-300 p-3 rounded-lg text-sm">
            {successMessage}
          </div>
        )}

        {/* Error Display */}
        {error && (
          <div className="mb-6 bg-red-900/30 border border-red-800 text-red-300 p-3 rounded-lg text-sm whitespace-pre-line">
            {error}
          </div>
        )}

        {/* Password requirements hint for registration */}
        {!isLogin && (
          <div className="mb-4 text-xs text-gray-400 bg-gray-800/50 p-3 rounded-lg">
            <p className="font-semibold mb-1">Password requirements:</p>
            <ul className="list-disc list-inside space-y-1">
              <li>At least 6 characters long</li>
              <li>At least one uppercase letter (A-Z)</li>
              <li>At least one lowercase letter (a-z)</li>
              <li>At least one number (0-9)</li>
            </ul>
          </div>
        )}

        {/* Form */}
        <form onSubmit={handleSubmit} className="space-y-4">
          {!isLogin && (
            <>
              <div>
                <label className="block text-gray-400 text-sm mb-1">
                  First Name *
                </label>
                <input
                  type="text"
                  name="firstName"
                  value={formData.firstName}
                  onChange={handleChange}
                  required
                  minLength="2"
                  maxLength="50"
                  className="w-full bg-[#1f2937] border border-gray-700 rounded-lg px-4 py-2 text-white focus:outline-none focus:ring-2 focus:ring-emerald-500"
                />
              </div>
              <div>
                <label className="block text-gray-400 text-sm mb-1">
                  Last Name *
                </label>
                <input
                  type="text"
                  name="lastName"
                  value={formData.lastName}
                  onChange={handleChange}
                  required
                  minLength="2"
                  maxLength="50"
                  className="w-full bg-[#1f2937] border border-gray-700 rounded-lg px-4 py-2 text-white focus:outline-none focus:ring-2 focus:ring-emerald-500"
                />
              </div>
            </>
          )}

          <div>
            <label className="block text-gray-400 text-sm mb-1">
              Email *
            </label>
            <input
              type="email"
              name="email"
              value={formData.email}
              onChange={handleChange}
              required
              className="w-full bg-[#1f2937] border border-gray-700 rounded-lg px-4 py-2 text-white focus:outline-none focus:ring-2 focus:ring-emerald-500"
            />
          </div>

          <div>
            <label className="block text-gray-400 text-sm mb-1">
              Password *
            </label>
            <input
              type="password"
              name="password"
              value={formData.password}
              onChange={handleChange}
              required
              minLength="6"
              className="w-full bg-[#1f2937] border border-gray-700 rounded-lg px-4 py-2 text-white focus:outline-none focus:ring-2 focus:ring-emerald-500"
            />
          </div>

          {!isLogin && (
            <div>
              <label className="block text-gray-400 text-sm mb-1">
                Confirm Password *
              </label>
              <input
                type="password"
                name="confirmPassword"
                value={formData.confirmPassword}
                onChange={handleChange}
                required
                minLength="6"
                className="w-full bg-[#1f2937] border border-gray-700 rounded-lg px-4 py-2 text-white focus:outline-none focus:ring-2 focus:ring-emerald-500"
              />
            </div>
          )}

          <button
            type="submit"
            disabled={loading}
            className="w-full bg-emerald-600 hover:bg-emerald-700 text-white font-medium py-2 rounded-lg transition disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {loading ? "Please wait..." : (isLogin ? "Login" : "Register")}
          </button>
        </form>

        {/* Toggle between Login/Register */}
        <div className="mt-6 text-center text-gray-400">
          {isLogin ? "Don't have an account? " : "Already have an account? "}
          <button
            onClick={() => {
              setIsLogin(!isLogin);
              setError("");
              setSuccessMessage("");
              setFormData({
                firstName: "",
                lastName: "",
                email: "",
                password: "",
                confirmPassword: ""
              });
            }}
            className="text-emerald-400 hover:underline"
          >
            {isLogin ? "Register" : "Login"}
          </button>
        </div>
      </div>
    </div>
  );
}