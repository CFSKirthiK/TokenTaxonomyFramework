# TTF Tools

## Taxonomy Service

- [Taxonomy Service](TaxonomyService/TaxonomyService) is a gRpc service that provides CRUD (create, read, update and delete) capabilities for the repo.  You can run this service from your local clone or use the published service from the [TTI Site](http://tokentaxonomy.org) once it is published. When the service starts, it is passed the path to the artifacts folder and then reads the taxonomy folder structure and files into the [Taxonomy Model](./taxonomy-model.md).  

- [Taxonomy Client](TaxonomyService/TaxonomyClient) is a simple command line tool that demonstrates how to interact with the Taxonomy Service to fetch the model and then apply it to any type of application for display or input.

- [Taxonomy Printer](TaxonomyService/TTF-Printer) a console based application that can generate OpenXML Wordprocessing documents from artifacts in the repo. It is particularly useful for creating a presentable view of to Token Specification (template). It requires that the Taxonomy Service be running and accessible.

- From your cloned copy of the repo.

```bash
cd tools/TaxonomyService
./docker-build.sh
 ```

This will build the Client and Service, starting the later awaiting client requests.  You can now start testing out the taxonomy.

### Test the Taxonomy

- To fetch the entire taxonomy model (using Docker to host the client):

Windows & Mac:

```bash
docker run -e gRpcHost=host.docker.internal txclient --f
```

Linux:

As `host.docker.internal` is currently not supported by Docker for Linux, the IP address of the docker bridge has to be retrieved first.

```bash
DOCKER_BRIDGE_IP=`docker network inspect bridge --format='{{(index .IPAM.Config 0).Gateway}}'`
docker run -e gRpcHost=$DOCKER_BRIDGE_IP -env printHost=$DOCKER_BRIDGE_IP txclient --f
```

if using the dotnet client using the [dotnet core runtime](https://dotnet.microsoft.com/download):

`dotnet TaxonomyClient --f`

### Options for the Client

- `--f`: must be used as a single argument and fetches the entire Taxonomy Model and writes it to the console.
- `--ts`: option indicating the artifact symbol. Value should be the letter or acronym for the artifact.
- `--t`: option for the artifact type. Valid values are 0 = Base, 1 = Behavior, 2 = BehaviorGroup, 3 = PropertySet or 4 - TokenTemplate
- `--s`: option to save a queried artifact so you may edit it locally. No value is set after the option
- `--u`: option to update a saved local artifact to the taxonomy. Value is the name of the folder to be used for the update. This is the folder created by the --s option and should be a relative path from where you are executing the command.
- `--c`: option to create a new artifact. Value is a proposed symbol for the artifact.
- `--n`: option for the name of a new artifact, used with --c.  Value is the name of the artifact.
- `--d`: create a template definition from a template formula, requires `--n`
- `--s id`: retrieves a TokenSpecification from a Template Definition Id from the Service.

Examples:

- `--ts m --t 1` is a query for a behavior with the symbol `m`, or mintable.
- `--ts SC --t 2` is a query for a behavior-group with the symbol `SC`, or supply-control.
- `--ts r --t 1 --s` is a query for a behavior with the symbol `r` and save it locally.  This will fetch the roles behavior and save it in a folder `roles`.
- `--u roles --t 1` will update the artifact from the roles folder saved in the previous example.
- `--c phSKU --n sku --t 3` will create a new artifact, a behavior-set, called `sku` with a symbol phSKU.
- `--d 89ff775c-27f1-494e-b31c-f3fb3a9527ac --n InvoiceToken` will create a template definition called InvoiceToken from the template formula with the UUID/Guid after the `--d`.

## Artifact Generator

[Artifact Generator](artifactGenerator) is a simple artifact generator to create stubbed artifacts of a particular type.  This is a console based application that takes 3 arguments, a relative path to the TTF [artifacts](../artifacts) folder, an artifact name and artifact type.  Artifact types are: 0 = Base, 1 = Behavior, 2 = BehaviorGroup, 3 = PropertySet or 4 - TokenTemplate

```bash
dotnet factgen --p ../artifacts --n myArtifactName --type 1
```

The above creates a folder, if it doesn't already exist, in the artifacts folder for the type of artifact and the name of the artifact.  In this folder you will find a Json definition, proto control and md for diagrams.

## TTF-Printer

The TTF-Printer runs as a service and uses the Taxonomy Service to create OpenXml Word Processing documents for Microsoft Word, Goggle Docs, etc.

It can print individual artifacts, which it creates in the artifacts folder or overwrite to a .docx file. You can also print all TTF artifacts which will create print out OpenXml in each artifacts folder or create/overwrite a TTF-Book.docx in the root of the repo.

The TTF-Printer can be tested using the TaxonomyClient to print a single artifact given the artifact Id using `-Id` and type `-t` where the types are 0=Base, 1=Behavior, 2-BehaviorGroup, 3=PropertySet, 4=TemplateFormula, 5=TemplateDefinition, 6=TokenTemplate/Specification.  Example below prints a specification with the Id `3b557279-5400-472e-a68e-feb818930276`

```bash
dotnet TTF-Printer -id 3b557279-5400-472e-a68e-feb818930276 -t 6
```

Artifacts are printed in their respective folder with a `.docx` extension and can be edited with most Word Processing documents. If you find typos or errors, DO NOT modify the `.docx` file, but the `.json` file where the error occurs and re-print the artifact.

You can print all the TTF artifacts using the `-a` switch.

To print the entire TTF as a single `book`, use the `-b` switch, which will create or overwrite the `TTF-Book.docx` in the root of the repo. You will want to edit this document to add a table of contents and page numbers using the word processor of choice and then save the `book` elsewhere so it will not get overwritten.

These documents can be saved as .PDF using your word processing application as well.
