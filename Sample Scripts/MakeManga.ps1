<# comment

Structure srcDir as follows:

srcDir
|-series 1 dir
   |-chapter 1 dir
   |-chapter 2 dir
   |-chapter 3 dir
|-series 2 dir
   |-chapter 1 dir
   |-chapter 2 dir

and so forth

Create Ingo.json fles in each manga series top directory you want to process using the below JSON template:
If a myanimelistId is specified the API will be used to populate fields below the break.

{
    "rtl" : true,
    "myanimelistId" : null,
    "publisher" : "",

    "author" : "",
    "tags" : "",
    "description" : ""
}

Common metadata will be taken from there.
Titles with volume numbering will be generated automatically, so make sure you have chapter folders properly sorted in alphabetical order.

contines #>

$cpubmakePath ="path/to/cpubmake"

$srcDir = "in/dir"
$destDir = "out/dir"

ls $srcDir -Directory | foreach {
    $mangaRoot = $_.FullName
    $rootName = $_.Name
    $infoFile = "$($mangaRoot)\Info.json"
    $targetDir = "$destDir\$rootName"

    if(test-path -path $infoFile) {
        if(-not (Test-Path -Path $targetDir)) {
            mkdir $targetDir > $null
        }
        
        $infoData = Get-Content -path $infoFile | ConvertFrom-Json

        if($infoData.myanimelistId) {
            $malResponse = Invoke-WebRequest "https://api.jikan.moe/v3/manga/$($infoData.myanimelistId)" | ConvertFrom-Json           
            $infoData.author = $malResponse.authors[0].name
            $infoData.author = $infoData.author -split ", "
            $infoData.author = "$($infoData.author[1]) $($infoData.author[0])"
            $infoData.description = $malResponse.synopsis -replace '"', '""'
            $infoData.tags = ($malResponse.genres.name | Join-String -Separator ",") -replace '"', '""'
            $infoData | ConvertTo-Json > $infoFile
        }

        $volumeDirs = ls $mangaRoot -Directory
        $counter = 1;
        $volumeDirs | foreach {
            if($_.Name -match "^__") {
                $outName = ($_.Name -replace "^__", "")
            }
            elseif ($volumeDirs.Length -eq 1) {
                $outName = $rootName
            }
            else {
                $outName = "{0:D2}" -f $counter
                $outName = "$rootName vol. $outName"
                $counter++;
            }

            $rtlSwitch = ""
            if($infoData.rtl) {
                $rtlSwitch = "-rtl"
            }

            $metaSwitch = ""
            if($infoData.myanimelistId) {
                $metaSwitch = "--meta myanimelistid=$($infoData.myanimelistId)"
            }

            $outPath = "$($targetDir)\$($outName).epub"
            if(Test-Path -Path $outPath) {
                echo "$outPath found - skipping"
            }
            else {
                &$cpubmakePath -d "$($_.FullName)" $rtlSwitch --title "$outName" -o "$outPath" --author "$($infoData.author)" --publisher "$($infoData.publisher)" --tags "$($infoData.tags)" --description "$($infoData.description)" $metaSwitch       
            }
        }
    }
}