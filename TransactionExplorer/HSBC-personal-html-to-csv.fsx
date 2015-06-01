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

#r @"../packages/FSharp.Data.2.2.0/lib/net40/FSharp.Data.dll"

open FSharp.Data
open System
open System.IO

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
This function returns an Async computation that will write the rows to a file
*)
let WriteToFileAsync (output_filename:string) (rows:string[]) = 
    async{
        use file = File.CreateText(output_filename)
        return rows 
                |> Array.reduce(fun a s-> a + "\n" + s) 
                |> file.WriteAsync
    }

(**
Usually you'd have a few html files to convert to csv. In that case use the following
to do them all in parallel.
*)


let base_folder = @"c:\statements" //this is where the html files are and the output would be saved
let html_files = System.IO.Directory.GetFiles(base_folder,"*.html")

html_files
|> Array.map(fun f->
    async{
        let filename = System.IO.Path.GetFileNameWithoutExtension(f)
        let output_filename = System.IO.Path.Combine(base_folder, filename + ".csv")
        let rows = ConvertHtmlToCSV f
        return WriteToFileAsync output_filename rows
    }
)
|> Async.Parallel
|> Async.RunSynchronously
|> ignore
