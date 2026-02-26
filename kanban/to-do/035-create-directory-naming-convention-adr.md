# 035: Create Directory Naming Convention ADR

## Description

Create a formal Architectural Decision Record (ADR) documenting the project's directory naming conventions, rationale for technology-specific adaptations, and guidelines for future directory creation.

## Parent

029_Create-Directory-Naming-Convention-Analysis-Report

## Requirements

- Create formal ADR documenting directory naming decisions
- Establish clear rules for when to use PascalCase vs technology-specific conventions
- Document the rationale behind adaptive naming strategy
- Provide decision tree for future directory naming choices
- Include examples and counter-examples

## Background

The directory naming analysis revealed a sophisticated adaptive naming strategy that balances consistency with technology-appropriate conventions. This needs to be formally documented as an ADR to:

1. **Preserve institutional knowledge** about naming decisions
2. **Guide future contributors** on naming conventions
3. **Document rationale** for technology-specific adaptations
4. **Establish clear decision criteria** for edge cases

## ADR Content Requirements

### 1. Decision Statement
Clear statement of the directory naming approach adopted by the project.

### 2. Context and Problem Statement
- Multi-technology project with .NET, web, and infrastructure components
- Need for consistency vs technology ecosystem compatibility
- Developer cognitive load considerations
- Tool integration requirements

### 3. Decision Drivers
- .NET ecosystem conventions (PascalCase)
- Technology-specific tooling requirements (Kubernetes, JavaScript)
- Developer experience and navigation
- Build system and IDE integration
- Industry standards and best practices

### 4. Considered Options
- **Option A**: Strict PascalCase everywhere
- **Option B**: Technology-specific conventions everywhere
- **Option C**: Adaptive strategy (chosen)

### 5. Decision Outcome
Document the chosen adaptive naming strategy with:

#### Primary Convention: PascalCase
- Default for all .NET projects and general organization
- Examples: `Features/`, `Components/`, `Common.Application/`

#### Technology-Specific Adaptations
- **Kubernetes**: kebab-case for resources (`api-server/`), numbered snake_case for organization (`0_Namespaces/`)
- **JavaScript/TypeScript**: camelCase for directories (`features/`, `types/`)
- **Infrastructure**: Tool-appropriate naming (domain names, etc.)

### 6. Decision Rules and Guidelines

#### When to Use PascalCase (Default)
- .NET project directories
- Documentation organization
- General feature organization
- Component hierarchies

#### When to Deviate to Technology-Specific Conventions
- **Kubernetes resource directories**: Use kebab-case (`api-server/`, `web-server/`)
- **Kubernetes organization**: Use numbered snake_case (`0_Namespaces/`, `4_Storage/`)
- **JavaScript asset directories**: Use camelCase (`features/`, `types/`)
- **Domain-based paths**: Use lowercase with dots (`timewarp.software.com/`)

### 7. Decision Tree
```
New Directory Creation:
├── Is this a Kubernetes resource directory?
│   └── Yes → Use kebab-case (api-server, grpc-server)
├── Is this Kubernetes organizational structure?
│   └── Yes → Use numbered snake_case (0_Namespaces, 4_Storage)
├── Is this JavaScript/TypeScript asset directory?
│   └── Yes → Use camelCase (features, types)
├── Is this a domain name or external tool requirement?
│   └── Yes → Follow external convention
└── Default → Use PascalCase
```

### 8. Rationale for Adaptive Strategy

#### Benefits
- **Respects ecosystem conventions** for each technology
- **Optimizes tool integration** (VSCode Kubernetes plugin, etc.)
- **Reduces cognitive friction** for specialists in each domain
- **Maintains .NET consistency** where appropriate

#### Trade-offs
- **Slightly more complex** than single convention
- **Requires documented guidelines** for decision-making
- **Context-dependent** naming choices

### 9. Examples and Counter-Examples

#### Correct Examples
```
Source/Common/Common.Application/      # PascalCase for .NET
DevOps/Kubernetes/0_Namespaces/       # Numbered snake_case for K8s org
DevOps/Kubernetes/2_Workloads/Deployments/api-server/  # kebab-case for K8s resources
Web.Spa/source/features/              # camelCase for JS assets
Documentation/Developer/HowToGuides/  # PascalCase for docs
```

#### Incorrect Examples
```
Source/common/common.application/     # Wrong: Should be PascalCase
DevOps/Kubernetes/ApiServer/          # Wrong: Should be kebab-case
Web.Spa/Source/Features/              # Wrong: Should be camelCase for JS
Documentation/developer/how-to-guides/ # Wrong: Should be PascalCase
```

### 10. Consequences

#### Positive
- Clear guidance for contributors
- Documented rationale for naming decisions
- Reduced naming bikeshedding in code reviews
- Consistent application of adaptive strategy

#### Negative
- Additional documentation to maintain
- Requires adherence to established patterns

## Implementation Tasks

### Phase 1: ADR Creation
- [ ] Create ADR file in `Documentation/Developer/Conceptual/ArchitecturalDecisionRecords/Approved/`
- [ ] Follow existing ADR template and numbering
- [ ] Include all sections outlined above
- [ ] Provide comprehensive examples

### Phase 2: Reference Documentation
- [ ] Update `CLAUDE.md` to reference the directory naming ADR
- [ ] Add quick reference guide for common scenarios
- [ ] Link ADR from relevant documentation sections

### Phase 3: Validation
- [ ] Review ADR with current directory structure
- [ ] Ensure all naming decisions are covered
- [ ] Verify decision tree handles edge cases
- [ ] Test guidelines against actual project structure

## Success Criteria

1. **Complete ADR** following project template and standards
2. **Clear decision tree** for future directory naming
3. **Comprehensive examples** covering all technology domains
4. **Documented rationale** for adaptive strategy choices
5. **Reference documentation** updated to point to ADR
6. **Validated against current structure** - no contradictions

## File Location

**ADR File**: `Documentation/Developer/Conceptual/ArchitecturalDecisionRecords/Approved/ADR-00XX-Directory-Naming-Conventions.md`

(Where XX is the next available ADR number)

## Notes

This ADR will codify the sophisticated directory naming strategy identified in the analysis, providing clear guidance for future development and ensuring the rationale for technology-specific adaptations is preserved.

**Key Value**: Transforms ad-hoc naming decisions into documented architectural choices with clear criteria for application.