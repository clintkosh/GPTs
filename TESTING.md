# Pre-production testing gate

The running quota notification extension must remain on a test branch until:

- manifest validation passes
- JavaScript syntax checks pass
- quota state calculations pass
- unavailable mode is verified
- estimated mode counting is verified
- daily reset behavior is verified
- threshold notifications are deduplicated
- keyboard and screen-reader attributes are verified
- manual browser smoke testing passes on ChatGPT

Do not merge or advertise the extension as production-ready until these checks pass.
