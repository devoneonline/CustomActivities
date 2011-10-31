﻿//-----------------------------------------------------------------------
// <copyright file="NewDeployment.cs">(c) http://TfsBuildExtensions.codeplex.com/. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace TfsBuildExtensions.Activities.Azure.HostedServices
{
    using System;
    using System.Activities;
    using System.ServiceModel;
    using Microsoft.Samples.WindowsAzure.ServiceManagement;
    using Microsoft.TeamFoundation.Build.Client;
    using TfsBuildExtensions.Activities.Azure.Helpers;

    /// <summary>
    /// Create a new deployment for a package which has already been uploaded.
    /// </summary>
    [BuildActivity(HostEnvironmentOption.All)]
    public class NewDeployment : BaseAzureActivity
    {
        /// <summary>
        /// Gets or sets the Azure deployment slot identifier.
        /// </summary>
        public InArgument<string> Slot { get; set; }

        /// <summary>
        /// Gets or sets the Azure service name.
        /// </summary>
        public InArgument<string> ServiceName { get; set; }

        /// <summary>
        /// Gets or sets the package location.
        /// This parameter should have the path or URI to a .cspkg in blob storage whose storage account is part of the same subscription/project.
        /// </summary>
        public InArgument<string> PackageUrl { get; set; }

        /// <summary>
        /// Gets or sets the configuration file path.
        /// This parameter should specifiy a .cscfg file on disk.
        /// </summary>
        public InArgument<string> ConfigurationFilePath { get; set; }

        /// <summary>
        /// Gets or sets the label name for the new deployment.
        /// </summary>
        public InArgument<string> DeploymentLabel { get; set; }

        /// <summary>
        /// Gets or sets the Azure deployment name.
        /// </summary>
        public InArgument<string> DeploymentName { get; set; }

        /// <summary>
        /// Gets or sets the operation id of the Azure API command.
        /// </summary>
        public OutArgument<string> OperationId { get; set; }

        /// <summary>
        /// Connect to an Azure subscription create a new deployment.
        /// </summary>
        protected override void AzureExecute()
        {
            var deploymentInput = this.CreateDeploymentInput();

            using (new OperationContextScope((IContextChannel)Channel))
            {
                try
                {
                    this.RetryCall(s => this.Channel.CreateOrUpdateDeployment(s, this.ServiceName.Get(this.ActivityContext), this.Slot.Get(this.ActivityContext), deploymentInput));
                    this.OperationId.Set(this.ActivityContext, RetrieveOperationId());
                }
                catch (EndpointNotFoundException ex)
                {
                    LogBuildMessage(ex.Message);
                    this.OperationId.Set(this.ActivityContext, null);
                }
            }
        }

        private CreateDeploymentInput CreateDeploymentInput()
        {
            string deploymentName = this.DeploymentName.Get(this.ActivityContext);
            if (string.IsNullOrEmpty(deploymentName))
            {
                deploymentName = Guid.NewGuid().ToString();
            }

            string package = this.PackageUrl.Get(this.ActivityContext); 
            Uri packageUrl;
            if (package.StartsWith(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                package.StartsWith(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                packageUrl = new Uri(package);
            }
            else
            {
                throw new InvalidOperationException("You must upload the blob to Azure before creating a new deployment.");
            }

            return new CreateDeploymentInput
            {
                PackageUrl = packageUrl,
                Configuration = Utility.GetConfiguration(this.ConfigurationFilePath.Get(this.ActivityContext)),
                Label = ServiceManagementHelper.EncodeToBase64String(this.DeploymentLabel.Get(this.ActivityContext)),
                Name = deploymentName
            };
        }
    }
}
