#r @"..\packages\FSharp.Data.2.2.0\lib\net40\FSharp.Data.dll"
open FSharp.Data
open System
open System.IO

(*  HSBC Personal Banking doesn't offer a way to download transactions in csv
    format. This script converts transactions in a HSBC statement webpage to 
    csv. 
    To use this, first login to personal banking goto statements and save
    statement webpages as you navigate them.
*)

let transactionTable (html:HtmlDocument) = 
    html.Descendants ["table"]
    |> Seq.choose(fun x->
        x.TryGetAttribute("summary")
        |> Option.map(fun a -> x, a.Value())
    )
    |> Seq.filter(fun (n,k)-> k.Contains("This table contains a statement of your account"))
    |> Seq.map(fun (n,k) -> n)
    |> Seq.head

let headers (table:HtmlNode) = 
    table |> fun k-> k.Descendants ["thead"] |> Seq.head
    |> fun x -> x.Descendants["tr"] |> Seq.head
    |> fun x -> x.Descendants["th"] |> Seq.map(fun x -> x.InnerText().Trim())

let transactionRows (table:HtmlNode)=
    table |> fun k-> k.Descendants ["tbody"] |> Seq.head
    |> fun x -> x.Descendants["tr"]
    |> Seq.map(fun tr -> 
        tr.Descendants["td"] 
        |> Seq.map(fun x -> System.Net.WebUtility.HtmlDecode(x.InnerText().Trim()) )
    )

//csv    
let ConvertHtmlToCSV (html_file:string) (csv_file:string) =
    let html = HtmlDocument.Load(html_file)
    let table = transactionTable html
    let h = headers table |> String.concat ","
    let rows = transactionRows table |> Seq.map(fun r -> r |> String.concat ",")
    let csv =  rows |> Seq.append (seq[h;]) |> Seq.toArray
    csv

    


