#load @"..\packages\FSharp.Formatting.2.8.0\FSharp.Formatting.fsx"
open FSharp.Literate
open System.IO

// ----------------------------------------------------------------------------
// SETUP
// ----------------------------------------------------------------------------

/// Return path relative to the current file location
let relative subdir = Path.Combine(__SOURCE_DIRECTORY__, subdir)


/// Processes a single F# Script file and produce HTML output
let file = relative "HSBC-personal-html-to-csv.fsx"
let output = relative "docs/HSBC-personal-html-to-csv.html"
let template = relative @"../templates/template-file.html"
Literate.ProcessScriptFile(file, template, output)

