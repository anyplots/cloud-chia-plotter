#How to run it?
#when select this file(gui.ps1), click the right button of the mouse, then select run with PowerShell on the right menu.
#for more usage & tips, go https://github.com/anyplots/cloud-chia-plotter 

[void] [System.Reflection.Assembly]::LoadWithPartialName("System.Drawing")
[void] [System.Reflection.Assembly]::LoadWithPartialName("System.Windows.Forms")



$path = (Split-Path -Parent $MyInvocation.MyCommand.Definition) + "\CloudChiaPlotter.exe"
$tmppath = (Split-Path -Parent $MyInvocation.MyCommand.Definition) + "\CloudChiaPlotter.tmp"

if(Test-Path $path )
{
    echo "CloudChiaPlotter.exe already was downloaded, if you want to update it, please delete it and run again."
}
else
{
    echo "CloudChiaPlotter.exe Downloading"
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Invoke-WebRequest -Uri  https://github.com/anyplots/cloud-chia-plotter/releases/download/v3/cloud-chia-plotter-v3-win-x64.exe -Outfile "$tmppath"
    Rename-Item -Path "$tmppath" -NewName "$path"
}


$objForm = New-Object System.Windows.Forms.Form
$objForm.Text = "Cloud Chia Plotter"
$objForm.Size = New-Object System.Drawing.Size(400,250)
$objForm.StartPosition = "CenterScreen"
$objForm.KeyPreview = $True

$objTokenLabel = New-Object System.Windows.Forms.Label
$objTokenLabel.Location = New-Object System.Drawing.Size(10,20)
$objTokenLabel.Size = New-Object System.Drawing.Size(280,20)
$objTokenLabel.Text =  "Enter your project token 40 chars:"
$objForm.Controls.Add($objTokenLabel)


$objTokenText = New-Object System.Windows.Forms.TextBox
$objTokenText.Location = New-Object System.Drawing.Size(10,40)
$objTokenText.Size = New-Object System.Drawing.Size(360,20)
$objTokenText.Text = ""
$objForm.Controls.Add($objTokenText)


$objDirsLabel = New-Object System.Windows.Forms.Label
$objDirsLabel.Location = New-Object System.Drawing.Size(10,70)
$objDirsLabel.Size = New-Object System.Drawing.Size(280,20)
$objDirsLabel.Text =  "Select some folders to save your plots:"
$objForm.Controls.Add($objDirsLabel)


$objDirsText = New-Object System.Windows.Forms.TextBox
$objDirsText.Location = New-Object System.Drawing.Size(10,90)
$objDirsText.Size = New-Object System.Drawing.Size(260,20)
$objDirsText.Text = ""
$objForm.Controls.Add($objDirsText)


$DirsButton = New-Object System.Windows.Forms.Button
$DirsButton.Location = New-Object System.Drawing.Size(280,90)
$DirsButton.Size = New-Object System.Drawing.Size(90,23)
$DirsButton.Text = "Add Folder ..."
$DirsButton.Add_Click({
    $objFolderForm = New-Object System.Windows.Forms.FolderBrowserDialog
    $objFolderForm.Description = "Select a folder to save chia plots:"
    if($objFolderForm.ShowDialog() -eq "OK"){
        if($objFolderForm.SelectedPath.Contains(" ")){
            [System.Windows.Forms.MessageBox]::Show("the selected path contains blank space, it was not supported: " + $objFolderForm.SelectedPath)
            return
        }
        if($objDirsText.Text -eq ""){
            $objDirsText.Text += $objFolderForm.SelectedPath;
        }
        else{
            $objDirsText.Text += ";" + $objFolderForm.SelectedPath;
        }
    }
})
$objForm.Controls.Add($DirsButton)


$objBandwidthLabel = New-Object System.Windows.Forms.Label
$objBandwidthLabel.Location = New-Object System.Drawing.Size(10,120)
$objBandwidthLabel.Size = New-Object System.Drawing.Size(380,20)
$objBandwidthLabel.Text =  "Set your ""real"" network bandwidth, such as ""500"" for 500Mbps"
$objForm.Controls.Add($objBandwidthLabel)
$objBandwidthLabel2 = New-Object System.Windows.Forms.Label
$objBandwidthLabel2.Location = New-Object System.Drawing.Size(115,143)
$objBandwidthLabel2.Size = New-Object System.Drawing.Size(40,20)
$objBandwidthLabel2.Text =  "Mbps"
$objForm.Controls.Add($objBandwidthLabel2)


$objBandwidthText = New-Object System.Windows.Forms.TextBox
$objBandwidthText.Location = New-Object System.Drawing.Size(10,140)
$objBandwidthText.Size = New-Object System.Drawing.Size(100,20)
$objBandwidthText.Text = ""
$objForm.Controls.Add($objBandwidthText)

$OKButton = New-Object System.Windows.Forms.Button
$OKButton.Location = New-Object System.Drawing.Size(150,170)
$OKButton.Size = New-Object System.Drawing.Size(75,23)
$OKButton.Text = "Start Now"
$OKButton.Add_Click({
    if( $objTokenText.Text.Length -ne 40){
        [System.Windows.Forms.MessageBox]::Show("invalid project token")
        return;
    }
    if( $objDirsText.Text.Length -eq 0){
        [System.Windows.Forms.MessageBox]::Show("you must add at least one directory to save your plots")
        return;
    }
    if( $objBandwidthText.Text.Length -eq 0){
        [System.Windows.Forms.MessageBox]::Show("you must set your real network bandwidth, you can test your speed at https://speedtest.net")
        return;
    }
    $objForm.DialogResult = "OK"
    $objForm.Close()
})
$objForm.Controls.Add($OKButton)

echo "Waiting for set parameters ..."
$objForm.Add_Shown({$objForm.Activate()})
if($objForm.ShowDialog() -eq "OK"){
    $args1 = $objTokenText.Text
    $args2 = $objDirsText.Text
    $args3 = $objBandwidthText.Text
    echo "path: $path"
    echo "-p $args1"
    echo "-d $args2"
    echo "-b $args3"
    Start-Process -NoNewWindow -Wait -FilePath "$path" -ArgumentList "-p $args1","-d $args2","-b $args3"
}
else{
    echo "Cancelled"
}
pause