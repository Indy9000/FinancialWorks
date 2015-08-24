(**
HSBC html statement converted to CSV
===============================
*)

(**  
HSBC Personal Banking doesn't offer a way to download transactions in csv
format. This F# script converts transactions found in a statement webpage to 
csv. 

How to use
----------
To use this, first login to personal banking and goto statements and save
statement webpages as you navigate them. Then load this script and call
*ConvertHtmlToCSV* with appropriate parameters
*)

// Step 0. Boilerplate to get the paket.exe tool
 
open System
open System.IO
 
Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

let dst = ".paket\paket.exe"
if not (File.Exists dst) then
    //let url = "https://github.com/fsprojects/Paket/releases/download/1.26.1/paket.exe"
    let urlRef = @"https://fsprojects.github.io/Paket/stable"
    use wc = new Net.WebClient()
    let url = wc.DownloadString(urlRef)
    let tmp = Path.GetTempFileName()
    wc.DownloadFile(url, tmp)
    Directory.CreateDirectory(".paket") |> ignore
    File.Move(tmp, dst)
 
// Step 1. Resolve and install the packages
 
#r ".paket\paket.exe"
 
Paket.Dependencies.Install """
source https://nuget.org/api/v2
nuget FSharp.Data
nuget Newtonsoft.Json
"""

// Step 2. Use the packages
 
#r @"packages\Newtonsoft.Json\lib\net45\Newtonsoft.Json.dll"
#r @"packages\FSharp.Data\lib\net40\FSharp.Data.dll"

open FSharp.Data
open System
open System.IO
open System.Threading
open System.Threading.Tasks
(**
Transactions are inside an html table. It looks as follows:
``
<table summary="This table contains a statement of your account">
...
</table>
``
We extract it using transacationTable 
*)
let transactionTable (html:HtmlDocument) = 
    html.Descendants ["table"]
    |> Seq.choose(fun n->
        n.TryGetAttribute("summary")
        |> Option.map(fun a -> n, a.Value())
    )
    |> Seq.filter(fun (n,av)-> av.Contains("This table contains a statement of your account"))
    |> Seq.map(fun (n,av) -> n)
    |> Seq.head

(**
We extract the headers for csv from the thead columns
*)
let headers (table:HtmlNode) = 
    table 
    |> fun k-> k.Descendants ["thead"] |> Seq.head
    |> fun x -> x.Descendants["tr"] |> Seq.head
    |> fun x -> x.Descendants["th"] |> Seq.map(fun x -> x.InnerText().Trim())

(**
We extract the table body and extract each row
*)
let transactionRows (table:HtmlNode)=
    table 
    |> fun k-> k.Descendants ["tbody"] |> Seq.head
    |> fun x -> x.Descendants["tr"]
    |> Seq.map(fun tr -> 
        tr.Descendants["td"] 
        |> Seq.map(fun x -> System.Net.WebUtility.HtmlDecode(x.InnerText().Trim()) )
    )
(**
We put all these together in the following that takes an html file and spits out
an array of csv rows
*)  
let ConvertHtmlToCSV (html_file:string) =
    let table = html_file |> HtmlDocument.Load |> transactionTable
    let h = table |> headers |> String.concat ","
    table
    |> transactionRows
    |> Seq.map(fun cols -> 
        cols
        // we use " to delimit a col that has , so replace them with '
        |> Seq.map(fun col -> col.Replace('"','''))
        //  if this col contains , then surround it with ""
        |> Seq.map(fun col -> if col.Contains(",") then ("\"" + col + "\"") else col )
        |> String.concat ","
        )
    |> Seq.append (seq[h;]) //append headers
    |> Seq.toArray

(**
Usually you'd have a few html files to convert to csv. In that case use the following
to do them all in parallel.
*)

let base_folder = @"c:\statements" //this is where the html files are and the output would be saved
let html_files = System.IO.Directory.GetFiles(base_folder,"*.html")

html_files
|> Array.iter(fun f->
    let filename = System.IO.Path.GetFileNameWithoutExtension(f)
    let output_filename = System.IO.Path.Combine(base_folder, filename + ".csv")
    if not <| File.Exists(output_filename) then
        let rows = ConvertHtmlToCSV f
        File.WriteAllLines(output_filename,rows)
        printfn "Created %s" output_filename
   )
