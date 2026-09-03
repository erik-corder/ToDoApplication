import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "To Do Application",
  description: "A to-do list, built on a Next.js + ASP.NET Core + PostgreSQL scaffold.",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="en" className="h-full antialiased">
      <body className="min-h-full flex flex-col font-sans">{children}</body>
    </html>
  );
}
