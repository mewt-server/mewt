# Mewt Server

> *Mewt Server* (or simply *Mewt*), is a minimalist and efficient webserver that render templates.

You want to create a landing page, a corporate website, something mostly static but you want to be able to update the content and the text efficiently?

But you don't want the complexity and the resources cost of a framework or CMS?

Mewt is made for YOU!

Mewt allows you to serve:

* Static files
* Pages generated with templates and variables
* Proxied APIs

Also, it handles for you redirects and URL rewriting.

## How to get it

* Binaries:
    * Hosted on GitHub: <https://github.com/mewt-server/mewt>
* Docker images:
    * Hosted on Docker Hub: <https://hub.docker.com/r/mewtserver/mewt>
    * `mewtserver/mewt:latest`
* Docker Compose:
   ```yml
   version: '3.8'
   
   services:
     mewt:
       image: mewtserver/mewt:latest
       ports:
         - 13523:13523
       restart: unless-stopped
       volumes:
         - cache:/mewt/cache:rw
         - mewt.yml:/mewt/mewt.yml:ro
         - source:/mewt/source:ro
   
   volumes:
     cache:
   ```

## How it works

### Typical folder structure

```yml
mewt/                       # Mewt root folder
├── cache/                  # Cache folder, must be writable
│   ├── metadata/           # Metadata of generated files
│   ├── private/            # Cache for APIs
│   └── public/             # Cache for Pages & Assets
├── source/                 # Source folder, can be read-only
│   ├── apis/           
│   │   └── proxy/          # Descriptors of proxied APIs
│   ├── assets/             # Assets files
│   ├── contents/           # Variable files, used by Pages for Templates
│   ├── pages/              # Descriptors for Pages
│   └── templates/          # Template files, used by Pages
└── mewt.yml                # Mewt Server configuration file
```

### Main configuration file

```yml
allowedHosts: "*"
applicationUrl: http://+:13523
debug: false
logging:
  logLevel:
    default: Warning
    mewt: Debug
    microsoft.aspNetCore: Warning
    microsoft.aspNetCore.hosting: Warning
    microsoft.hosting: Information
server:
  http:
    configureResponses: null
    redirects: []
    rewrites: []
    validateRequests: null
  paths:
    apis:
      path: source/apis
      updateCommand: &updateCommand |
        update = pwd | cmd.exec "git" "pull"
        response.status_code = update.exit_code == 0 ? 200 : 500
        update.standard_output + update.standard_error
    assets:
      path: source/assets
      updateCommand: *updateCommand
    contents:
      path: source/contents
      updateCommand: *updateCommand
    pages:
      path: source/pages
      updateCommand: *updateCommand
    metadata:
      provider: Memory
    private:
      provider: Memory
    public:
      provider: Memory
  swagger:
    enabled: false
```

### Assets

All files in the *assets* folder will be copied when their path is called.

### Page declaration syntax

Pages are declared to *Mewt* with yaml files:

```yml
content: {}                 # Object that can be used in templates to retrieve its values
contentFiles: []            # Files that provide YAML content to be used in templates
templateIncludes: []        # Templates that can be included using the `include` function in templates
templateFiles: []           # Scriban templates used to render the page
```

Example:

```yml
content:
  head:
    title: Homepage
  body:
    title: Lorem Ipsum !
    lines:
      - Lorem ipsum dolor sit amet, consectetur adipiscing elit.
      - Nulla non metus sed turpis venenatis placerat.
templateFiles:
  - main.html
```

### Template language syntax

*Mewt* uses [Scriban](https://github.com/scriban/scriban) scripting language to render templates,
full documentation and examples can be found at: <https://github.com/scriban/scriban/blob/master/doc/language.md>.
It also provides convenient built-in functions, described at: <https://github.com/scriban/scriban/blob/master/doc/builtins.md>.

Example:

```html
<html>
  <head>
    <meta charset="utf-8" />
    <title>{{ head.title }}</title>
  </head>
  <body>
    <h1>{{ body.title }}</h1>
    {{ for line in body.lines }}
    <p>{{ line }}</p>
    {{ end }}
  </body>
</html>
```

## Develop & Contribute

* Debug tests: `dotnet test --logger "console;verbosity=detailed"`
* Run tests with code coverage: `dotnet test --collect:"XPlat Code Coverage"`
* Analyse code coverage reports: `reportgenerator -reports:"test/TestResults/{guid}/coverage.cobertura.xml" -targetdir:"coveragereport"`, and open file `coveragereport/index.html`
* Build Docker (Alpine): `docker build -t mewtserver/mewt:dev-alpine -f docker/alpine/Dockerfile .`
* Build executable: `dotnet publish src/Mewt/mewt.csproj --configuration Release --output publish --self-contained`

## License

![AGPL-3.0-only](https://www.gnu.org/graphics/agplv3-155x51.png)

Mewt is licensed under the terms of the AGPL-3.0-only [license](LICENSE).

```txt
Mewt, a minimalist and efficient webserver rendering templates.
Copyright (C) 2023 Jérémy WALTHER <jeremy.walther@golflima.net>
Documentation and source code: <https://github.com/mewt-server/mewt>.

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as
published by the Free Software Foundation, version 3 of the License.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
```

It is uses following licensed material:

* [.NET Runtime, MIT](https://github.com/dotnet/runtime)
* [ASP.NET Core, MIT](https://github.com/dotnet/aspnetcore)
* [NetEscapades.Configuration, MIT](https://github.com/andrewlock/NetEscapades.Configuration)
* [scriban, BSD-2-Clause](https://github.com/scriban/scriban)
* [Swashbuckle.AspNetCore, MIT](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)
* [xUnit.net, Apache-2.0 + MIT](https://github.com/xunit/xunit)
* [YamlDotNet, MIT](https://github.com/aaubry/YamlDotNet)