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
we put all these together in the following that takes an html file and spits out
a csv file.
*)  
let ConvertHtmlToCSV (html_file:string) (csv_file:string) =
    let html = HtmlDocument.Load(html_file)
    let table = transactionTable html
    let h = headers table |> String.concat ","
    let rows = transactionRows table |> Seq.map(fun r -> r |> String.concat ",")
    let csv =  rows |> Seq.append (seq[h;]) |> Seq.toArray
    csv

    


