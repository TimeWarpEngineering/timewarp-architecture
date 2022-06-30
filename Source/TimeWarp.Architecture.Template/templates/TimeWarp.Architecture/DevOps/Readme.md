# DevOps

This folder is for all [DevOps](https://azure.microsoft.com/en-us/overview/what-is-devops/#devops-overview) related files.

Solution Stack

1. Infrastructure
2. Platform
3. Application

## Terms

Distributed Applications
: Software applications that are stored and executed mostly on cloud computing platforms and that run on multiple systems simultaneously. These distributed systems operate on the same network and communicate with each other in an effort to complete a specific task or command—unlike a traditional app, which utilizes one dedicated system to achieve an assigned task.

Solution Stack
: the entire collection of the Infrastructure, Platform, and Application layers to provide a particular Distributed Application Solution.

Infrastructure
: containing bare metal instances, virtual machines, networking, firewall, security etc.

Platform
: the OS, runtime environment, development tools, pipelines etc.

Application
: contains your application code and data

A typical operations team works on the provisioning, monitoring and management of the infra and platform layers, in addition to enabling the deployment of code.

## Software Tools by stack layer

### Infrastructure tools

* Bicep (Azure specific DSL)
* Pulumi (Supports multi cloud, but requires different code per cloud) Also can be run like a service to facilitate self deployment. Example a web app could launch deployment.

### Platform tools

* az cli
* bicep cli
* Docker
* dotnet
* GitHub Actions
* Tye
* TypeScript
* Visual Studio

### Application Items

* source
* data
* assets (images, translations etc...)

## Tasks

- [ ] 


## References

https://www.hyscale.io/blog/infrastructure-as-code-vs-platform-as-code/
https://azure.microsoft.com/en-us/overview/what-is-devops/#devops-overview
